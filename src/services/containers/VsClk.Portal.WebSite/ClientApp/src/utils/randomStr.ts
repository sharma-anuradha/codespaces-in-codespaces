export const randomStr = (charCount = 36) => {
    return Math.random().toString(charCount).substring(2) + Date.now().toString(charCount);
}