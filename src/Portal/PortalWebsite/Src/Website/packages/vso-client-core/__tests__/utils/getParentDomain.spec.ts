import { getParentDomain } from '../../src/utils/getParentDomain';

describe('getParentDomain', () => {
    it('should get top level domain', () => {
        expect(getParentDomain('https://github.com')).toBe('github.com');
    });

    it('should get top level domain from URL with 3 levels', () => {
        expect(getParentDomain('https://one.dev.github.com', 3)).toBe('dev.github.com');
    });

    it('should get top level domain from URL with 4 levels', () => {
        expect(getParentDomain('https://one.dev.github.com', 4)).toBe('one.dev.github.com');
        expect(getParentDomain('https://two.one.dev.github.com', 4)).toBe('one.dev.github.com');
    });

    it('should get top level domain from URL with 5 levels', () => {
        expect(getParentDomain('https://two.one.dev.github.com', 5)).toBe('two.one.dev.github.com');
        expect(getParentDomain('https://three.two.one.dev.github.com', 5)).toBe('two.one.dev.github.com');
        expect(getParentDomain('https://three.two.one.dev.github.com')).toBe('github.com');
    });

    it('should get top level domain when URL too short', () => {
        expect(getParentDomain('https://github.com', 3)).toBe('github.com');
        expect(getParentDomain('https://github.com', 5)).toBe('github.com');
        expect(getParentDomain('https://github.dev', 5)).toBe('github.dev');
        expect(getParentDomain('https://github.localhost', 5)).toBe('github.localhost');
    });
});
