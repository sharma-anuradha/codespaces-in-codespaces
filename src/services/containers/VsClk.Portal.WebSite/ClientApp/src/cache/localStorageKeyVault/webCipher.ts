import { Buffer } from 'buffer';

import { createTrace } from '../../utils/createTrace';

import { ICipher } from '../../interfaces/ICipher';

const trace = createTrace('WebCipher');

export type SupportedCipherAlgorithms = 'AES';
export type SupportedCipherModes = 'CTR' | 'CBC';

export class WebCipher implements ICipher {
	private key!: CryptoKey;
	private iv!: Buffer;

	public get blockLength() {
		return this.blockSizeInBits / 8;
	}

	public constructor(
		public readonly isEncryption: boolean,
		public readonly algorithmName: SupportedCipherAlgorithms,
		public readonly cipherMode: SupportedCipherModes,
		public readonly keySizeInBits: number,
		public readonly blockSizeInBits: number,
	) {
		if (this.algorithmName === 'AES' && this.cipherMode === 'CTR') {
			this.transform = this.aesCtr.bind(this, isEncryption);
			this.transform = this.aesCtr.bind(this, isEncryption);
		} else if (this.algorithmName === 'AES' && this.cipherMode === 'CBC') {
			this.transform = this.aesCbc.bind(this, isEncryption);
			this.transform = this.aesCbc.bind(this, isEncryption);
		} else {
			throw new Error(
				`Unsupported encryption algorithm: ${this.algorithmName}-${this.cipherMode}`,
			);
		}
	}

	public async init(key: Buffer, iv: Buffer): Promise<void> {
		try {
			const name = `${this.algorithmName}-${this.cipherMode}`;
			this.key = await crypto.subtle.importKey(
				'raw',
				key,
				<AesKeyAlgorithm>{ name, length: this.keySizeInBits },
				false,
				this.isEncryption ? ['encrypt'] : ['decrypt'],
			);
		} catch (e) {
			throw new Error('Failed to initialize AES: ' + e);
		}
		this.iv = Buffer.from(iv);
	}

	public readonly transform: (data: Buffer) => Promise<Buffer>;

	private async aesCtr(isEncryption: boolean, data: Buffer): Promise<Buffer> {
		if (data.length % this.blockLength !== 0) {
			const message =
				'Encrypt/decrypt input has invalid length ' +
				`${data.length}, not a multiple of block size ${this.blockLength}.`;
			trace.error('Error: ' + message);
			throw new Error(message);
		}

		let result: Buffer;
		if (isEncryption) {
			result = Buffer.from(
				await crypto.subtle.encrypt(
					{ name: 'AES-CTR', counter: this.iv, length: this.blockSizeInBits },
					this.key,
					data,
				),
			);
		} else {
			result = Buffer.from(
				await crypto.subtle.decrypt(
					{ name: 'AES-CTR', counter: this.iv, length: this.blockSizeInBits },
					this.key,
					data,
				),
			);
		}

		if (result.length !== data.length) {
			const message =
				'Result from encrypt/decrypt has invalid length ' +
				`${result.length}, expected ${data.length}.`;
			trace.error('Error: ' + message);
			throw new Error(message);
		}

		// A single call to encrypt() or decrypt() internally increments the counter.
		// This code ensures those increments get preserved across multiple calls.
		const incrementCount = data.length / this.blockLength;
		for (let i = 0; i < incrementCount; i++) {
			// Increment the counter that is combined with the IV as a big-endian integer.
			// First increment the last byte, and if it reaches 0 then increment the
			// next-to-last byte, and so on.
			for (let k = this.iv.length - 1; k >= 0; k--) {
				this.iv[k] = this.iv[k] + 1;
				if (this.iv[k]) break;
			}
		}

		return result;
	}

	private async aesCbc(isEncryption: boolean, data: Buffer): Promise<Buffer> {
		// CBC cannot be used to encrypt a sequence of buffers as a stream
		if (isEncryption) {
			let result = Buffer.from(
				await crypto.subtle.encrypt({ name: 'AES-CBC', iv: this.iv }, this.key, data),
			);
			return result;
		} else {
			const result = await crypto.subtle.decrypt({ name: 'AES-CBC', iv: this.iv }, this.key, data);
			return Buffer.from(result);
		}
	}
}
