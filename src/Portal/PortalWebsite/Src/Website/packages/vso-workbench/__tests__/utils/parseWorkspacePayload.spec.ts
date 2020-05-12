import { parseWorkspacePayload } from "../../src/utils/parseWorkspacePayload";

describe('parseWorkspacePayload', () => {
    it('should parse `payload`', () => {
        const payloadObject = [["a", "b"], ["c", "d"]];
        const payloadString = JSON.stringify(payloadObject);
        const parsed = parseWorkspacePayload(payloadString);

        expect(parsed).toEqual(payloadObject);
    });

    it('should not throw on bad `payload` #1', () => {
        const parsed = parseWorkspacePayload('asdasdad');

        expect(parsed).toEqual(null);
    });

    it('should not throw on bad `payload` #2', () => {
        const payloadString = undefined;
        const parsed = parseWorkspacePayload(payloadString as any);

        expect(parsed).toEqual(null);
    });

    it('should not throw on bad `payload` #3', () => {
        const payloadString = null;
        const parsed = parseWorkspacePayload(payloadString as any);

        expect(parsed).toEqual(null);
    });

    it('should parse only `entries` object #1', () => {
        const payloadString = '{}';
        const parsed = parseWorkspacePayload(payloadString);

        expect(parsed).toEqual(null);
    });

    it('should parse only `entries` object #2', () => {
        const payloadString = '[]';
        const parsed = parseWorkspacePayload(payloadString);

        expect(parsed).toEqual([]);
    });

    it('should parse only `entries` object #3', () => {
        const payloadString = '[["a", 2], [2, 2]]';
        const parsed = parseWorkspacePayload(payloadString);

        expect(parsed).toEqual(null);
    });
});
