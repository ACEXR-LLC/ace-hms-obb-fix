const HEADER_LENGTH = 24;
const READ_LENGTH = 16384;
const COMMAND_NAMES = { 0x4e584e43: "CNXN", 0x48545541: "AUTH", 0x4e45504f: "OPEN", 0x59414b4f: "OKAY", 0x45534c43: "CLSE", 0x45545257: "WRTE" };

export function commandName(command) {
  return COMMAND_NAMES[command] ?? `0x${(command >>> 0).toString(16)}`;
}

export class DeviceBusyError extends Error {
  constructor(cause) {
    super("The ADB interface is held by another program.");
    this.name = "DeviceBusyError";
    this.cause = cause;
  }
}

export async function openAdbConnection(usbDevice, events = {}) {
  await usbDevice.open();
  if (usbDevice.configuration === null) {
    await usbDevice.selectConfiguration(1);
  }
  const target = findAdbInterface(usbDevice);
  if (!target) {
    throw new Error("This device has no ADB interface. USB debugging is probably off.");
  }
  try {
    await usbDevice.claimInterface(target.usbInterface.interfaceNumber);
  } catch (error) {
    if (error?.name === "NetworkError") {
      throw new DeviceBusyError(error);
    }
    throw error;
  }
  if (target.usbInterface.alternate.alternateSetting !== target.alternate.alternateSetting) {
    await usbDevice.selectAlternateInterface(target.usbInterface.interfaceNumber, target.alternate.alternateSetting);
  }
  return new UsbAdbConnection(usbDevice, target, events);
}

function findAdbInterface(usbDevice) {
  for (const usbInterface of usbDevice.configuration.interfaces) {
    const alternate = usbInterface.alternates.find((candidate) =>
      candidate.interfaceClass === 255 && candidate.interfaceSubclass === 66 && candidate.interfaceProtocol === 1);
    if (alternate) {
      return { usbInterface, alternate };
    }
  }
  return undefined;
}

class UsbAdbConnection {
  #device;
  #interfaceNumber;
  #inEndpoint;
  #outEndpoint;
  #events;
  #buffer = new Uint8Array(0);
  #closed = false;
  #handshakeDone = false;

  constructor(usbDevice, target, events) {
    this.#device = usbDevice;
    this.#interfaceNumber = target.usbInterface.interfaceNumber;
    this.#inEndpoint = target.alternate.endpoints.find((endpoint) => endpoint.direction === "in");
    this.#outEndpoint = target.alternate.endpoints.find((endpoint) => endpoint.direction === "out");
    this.#events = events;
    this.readable = new ReadableStream({
      pull: async (controller) => {
        const packet = await this.#readPacket();
        if (packet) {
          controller.enqueue(packet);
        } else {
          controller.close();
        }
      },
      cancel: () => this.close(),
    }, { highWaterMark: 0 });
    this.writable = new WritableStream({
      write: (chunk) => this.#writePacket(chunk),
      close: () => this.close(),
      abort: () => this.close(),
    });
  }

  get description() {
    return `interface #${this.#interfaceNumber}, in${this.#inEndpoint.endpointNumber}/out${this.#outEndpoint.endpointNumber}, packet ${this.#inEndpoint.packetSize}`;
  }

  async close() {
    if (this.#closed) {
      return;
    }
    this.#closed = true;
    try {
      await this.#device.releaseInterface(this.#interfaceNumber);
    } catch {
    }
    try {
      await this.#device.close();
    } catch {
    }
  }

  async #writePacket(chunk) {
    const packet = isConsumable(chunk) ? chunk.value : chunk;
    const payload = packet.payload ?? new Uint8Array(0);
    const header = new ArrayBuffer(HEADER_LENGTH);
    const view = new DataView(header);
    view.setUint32(0, packet.command >>> 0, true);
    view.setUint32(4, packet.arg0 >>> 0, true);
    view.setUint32(8, packet.arg1 >>> 0, true);
    view.setUint32(12, payload.length, true);
    view.setUint32(16, checksum(payload), true);
    view.setUint32(20, (packet.command ^ 0xffffffff) >>> 0, true);
    await this.#transferOut(header);
    if (payload.length > 0) {
      await this.#transferOut(payload);
    }
    this.#events.onSent?.(commandName(packet.command));
    if (isConsumable(chunk)) {
      chunk.consume();
    }
  }

  async #transferOut(data) {
    const result = await this.#device.transferOut(this.#outEndpoint.endpointNumber, data);
    if (result.status !== "ok") {
      throw new Error(`USB write failed: ${result.status}`);
    }
    if (data.byteLength > 0 && data.byteLength % this.#outEndpoint.packetSize === 0) {
      await this.#device.transferOut(this.#outEndpoint.endpointNumber, new Uint8Array(0));
    }
  }

  async #readPacket() {
    while (!this.#closed) {
      const header = await this.#readBytes(HEADER_LENGTH);
      if (!header) {
        return undefined;
      }
      const view = new DataView(header.buffer, header.byteOffset, header.byteLength);
      const command = view.getUint32(0, true);
      const magic = view.getUint32(20, true);
      if (magic !== ((command ^ 0xffffffff) >>> 0)) {
        this.#events.onNoise?.();
        this.#buffer = this.#buffer.subarray(1);
        continue;
      }
      const packet = {
        command,
        arg0: view.getUint32(4, true),
        arg1: view.getUint32(8, true),
        payload: new Uint8Array(0),
      };
      const payloadLength = view.getUint32(12, true);
      this.#buffer = this.#buffer.subarray(HEADER_LENGTH);
      if (payloadLength > 0) {
        const payload = await this.#readBytes(payloadLength);
        if (!payload) {
          return undefined;
        }
        packet.payload = payload.slice();
        this.#buffer = this.#buffer.subarray(payloadLength);
      }
      const name = commandName(command);
      if (!this.#handshakeDone && name !== "CNXN" && name !== "AUTH") {
        this.#events.onStale?.(name);
        continue;
      }
      if (name === "CNXN") {
        this.#handshakeDone = true;
      }
      this.#events.onReceived?.(name);
      return packet;
    }
    return undefined;
  }

  async #readBytes(length) {
    while (this.#buffer.length < length) {
      if (this.#closed) {
        return undefined;
      }
      let result;
      try {
        result = await this.#device.transferIn(this.#inEndpoint.endpointNumber, Math.max(READ_LENGTH, length - this.#buffer.length));
      } catch (error) {
        if (this.#closed) {
          return undefined;
        }
        throw error;
      }
      if (result.status !== "ok") {
        throw new Error(`USB read failed: ${result.status}`);
      }
      const chunk = new Uint8Array(result.data.buffer, result.data.byteOffset, result.data.byteLength);
      if (chunk.length === 0) {
        continue;
      }
      const merged = new Uint8Array(this.#buffer.length + chunk.length);
      merged.set(this.#buffer);
      merged.set(chunk, this.#buffer.length);
      this.#buffer = merged;
    }
    return this.#buffer.subarray(0, length);
  }
}

function isConsumable(chunk) {
  return chunk !== null && typeof chunk === "object" && "value" in chunk && typeof chunk.consume === "function";
}

function checksum(payload) {
  let sum = 0;
  for (const byte of payload) {
    sum = (sum + byte) >>> 0;
  }
  return sum;
}
