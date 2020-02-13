export class ExponentialBackoff {
    private retries = 0;
    private pow = 1;

    constructor(
        private maxRetries: number,
        private delayMilliseconds: number,
        private maxDelayMilliseconds: number) {
    }

    public get retriesCount(): number {
        return this.retries;
    }

    public nextDelayMilliseconds(): number {
        if (this.maxRetries !== -1 && this.retries === this.maxRetries) {
            throw new Error('Max retry attempts exceeded.');
        }

        ++this.retries;
        if (this.retries < 31) {
            this.pow = this.pow << 1; // m_pow = Pow(2, m_retries - 1)
        }

        return Math.min(this.delayMilliseconds * (this.pow - 1) / 2,
            this.maxDelayMilliseconds);
    }
}