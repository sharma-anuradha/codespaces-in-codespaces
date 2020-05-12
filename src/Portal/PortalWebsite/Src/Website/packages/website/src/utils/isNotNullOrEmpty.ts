export function isNotNullOrEmpty<T>(value: string | T[] | undefined | null): value is T[];
export function isNotNullOrEmpty<T>(value: string | undefined | null): value is string;
export function isNotNullOrEmpty<T>(value: any): value is T[] {
    return value != null && value.length > 0;
}
