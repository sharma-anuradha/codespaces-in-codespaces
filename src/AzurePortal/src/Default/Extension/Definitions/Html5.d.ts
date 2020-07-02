interface Window {
    chrome: any;
    ActiveXObject: any;

    MsPortalFx: any;

    // Chrome
    OverflowEvent: any;
}

interface MessageEventListener extends EventListener {
    (evt: MessageEvent): void;
}

// The following parseInt is required here because lib.d supports only string as input.
// Even though TypeScript will not change the input to be any, it would be a big
// change in programming habits and language to transform all variables to a string
// before calling this function.
/**
 * Converts any into an integer.
 *
 * @param s A value to convert into a number.
 * @param radix A value between 2 and 36 that specifies the base of the number in numString.
 * If this argument is not supplied, strings with a prefix of '0x' are considered hexadecimal.
 * All other strings are considered decimal.
 * @return Input converted to a number.
 */
declare function parseInt(s: any, radix?: number): number;

interface StringMap<T> {
    [key: string]: T;
}

interface ReadonlyStringMap<T> {
    readonly [key: string]: T;
}

type ReadonlyTypedStringMap<K extends string, T> = {
    readonly [P in K]?: T;
};

type PartialTypedStringMap<K extends string, T> = {
    [P in K]?: T;
};

type Without<T, U> = {
    [P in Exclude<keyof T, keyof U>]?: never
};

type XOR<T, U> = (T | U) extends object ? (Without<T, U> & U) | (Without<U, T> & T) : T | U;

interface NumberMap<T> {
    [key: number]: T;
    length?: number;
}

interface ReadonlyNumberMap<T> {
    readonly [key: number]: T;
    readonly length?: number;
}

/**
 * Defines a type that is similar to T but with all of its members without the readonly modifier if present.
 * The Mutable modifier is shallow and does not apply recursively to complex properties.
 */
type Mutable<T> = {
    -readonly [P in keyof T] : T[P]
};

interface NameValue<N, T> {
    name: N;
    value: T;
}

/**
 * Supports simple toString() conversions.
 */
interface ConvertsToString {
    /**
     * Converts to string.
     *
     * @returns The string representation.
     */
    toString(): string;
}
