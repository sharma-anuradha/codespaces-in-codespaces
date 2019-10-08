/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * @hidden
 */
export class ClientInfo {

    private uidInternal: string | undefined = undefined;
    get uid(): string {
        return this.uidInternal ? this.uidInternal : "";
    }

    set uid(uid: string) {
        this.uidInternal = uid;
    }

    private utidInternal: string | undefined = undefined;
    get utid(): string {
        return this.utidInternal ? this.utidInternal : "";
    }

    set utid(utid: string) {
        this.utidInternal = utid;
    }

    constructor(rawClientInfo: string) {
        if (!rawClientInfo || !rawClientInfo) {
            this.uid = "";
            this.utid = "";
            return;
        }

        try {
            const decodedClientInfo: string = atob(rawClientInfo);
            const clientInfo: ClientInfo = <ClientInfo>JSON.parse(decodedClientInfo);
            if (clientInfo) {
                if (clientInfo.hasOwnProperty("uid")) {
                    this.uid = clientInfo.uid;
                }

                if (clientInfo.hasOwnProperty("utid")) {
                    this.utid = clientInfo.utid;
                }
            }
        } catch (e) {
            throw new Error(e);
        }
    }
}
