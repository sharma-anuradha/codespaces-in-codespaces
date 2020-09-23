import { appendUrlPath } from '../../src/utils/appendUrlPath';

describe('appendUrlPath', () => {
    it('should append a path to URL', () => {
        const url = 'https://foo.co'
        const path = '/api/v1/baz'

        const result = appendUrlPath(url, path);

        expect(result).toBe(`${url}${path}`);
    });

    it('should keep existing path', () => {
        const url = 'https://foo.co/proxy/v2'
        const path = '/api/v1/baz'

        const result = appendUrlPath(url, path);

        expect(result).toBe(`${url}${path}`);
    });

    it('should keep query params', () => {
        const query = '?param=value';
        const cleanUrl = `https://foo.co/proxy/v2`
        const url = `${cleanUrl}${query}`
        const path = '/api/v1/baz'

        const result = appendUrlPath(url, path);

        expect(result).toBe(`${cleanUrl}${path}${query}`);
    });

    it('should keep fragment', () => {
        const fragment = '#some-fragment';
        const cleanUrl = `https://foo.co/proxy/v2`
        const url = `${cleanUrl}${fragment}`
        const path = '/api/v1/baz'

        const result = appendUrlPath(url, path);

        expect(result).toBe(`${cleanUrl}${path}${fragment}`);
    });

    it('should keep query params and fragment', () => {
        const fragment = '#some-fragment&param1=value3';
        const query = '?param1=value2&param2=value1';
        const cleanUrl = `https://foo.co/proxy/v2`
        const url = `${cleanUrl}${query}${fragment}`
        const path = '/api/v1/baz'

        const result = appendUrlPath(url, path);

        expect(result).toBe(`${cleanUrl}${path}${query}${fragment}`);
    });
});
