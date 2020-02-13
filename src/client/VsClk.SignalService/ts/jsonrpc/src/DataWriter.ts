import { Buffer, TranscodeEncoding } from 'buffer';

export class DataWriter {
    public position: number = 0;

    public constructor(private buffer: Buffer) {}

    public write(data: Buffer) {
        this.ensureCapacity(this.position + data.length);

        data.copy(this.buffer, this.position);
        this.position += data.length;
    }

    public writeByte(value: number): void {
        this.ensureCapacity(this.position + 1);

        this.buffer[this.position] = value;
        this.position++;
    }

    public writeBinary(data: Buffer): void {
        this.writeUInt32(data.length);
        this.write(data);
    }

    public writeString(value: string, encoding: TranscodeEncoding): void {
        this.writeBinary(Buffer.from(value));
    }

    public writeList(value: string[], encoding: TranscodeEncoding) {
        this.writeString(value ? value.join(',') : '', encoding);
    }

    public writeBoolean(value: boolean): void {
        this.writeByte(value ? 1 : 0);
    }

    public writeUInt32(value: number): void {
        this.ensureCapacity(this.position + 4);

        this.buffer[this.position + 0] = value >> 24;
        this.buffer[this.position + 1] = value >> 16;
        this.buffer[this.position + 2] = value >> 8;
        this.buffer[this.position + 3] = value >> 0;
        this.position += 4;
    }

    public writeMpint(value: Buffer): void {
        if (value.length === 1 && value[0] === 0) {
            this.writeUInt32(0);
        } else {
            const high = (value[0] & 0x80) !== 0;
            if (high) {
                this.writeUInt32(value.length + 1);
                this.writeByte(0);
                this.write(value);
            } else {
                this.writeUInt32(value.length);
                this.write(value);
            }
        }
    }

    public skip(length: number): void {
        this.ensureCapacity(this.position + length);
        this.position += length;
    }

    private ensureCapacity(capacity: number): void {
        if (this.buffer.length < capacity) {
            let newLength = Math.max(512, this.buffer.length * 2);
            while (newLength < capacity) newLength *= 2;

            const newBuffer = Buffer.alloc(newLength);
            this.buffer.copy(newBuffer, 0, 0, this.position);
            this.buffer = newBuffer;
        }
    }

    public toBuffer(): Buffer {
        return this.buffer.slice(0, this.position);
    }
}