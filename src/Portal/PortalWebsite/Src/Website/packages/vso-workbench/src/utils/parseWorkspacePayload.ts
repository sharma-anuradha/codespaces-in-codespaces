const isEntry = (obj: any) => {
    if (!Array.isArray(obj)) {
        return false;
    }

    if (obj.length !== 2) {
        return false;
    }

    if (typeof obj[0] !== 'string') {
        return false;
    }

    return true;
}

const isEntries = (obj: any) => {
    if (!Array.isArray(obj)) {
        return false;
    }

    const isNotEntryElement = obj.find((entry) => {
        return !isEntry(entry);
    });

    return !isNotEntryElement;
};

export const parseWorkspacePayload = (payloadString: string | null): [string, any][] | null => {
    if (typeof payloadString !== 'string') {
        return null;
    }

    try {
        const parsed = JSON.parse(payloadString);
        if (!isEntries(parsed)) {
            return null;
        }

        return parsed;
    } catch {
        return null;
    }
};