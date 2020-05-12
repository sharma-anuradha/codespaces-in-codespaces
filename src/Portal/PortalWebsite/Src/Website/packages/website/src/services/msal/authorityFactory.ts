/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * @hidden
 */
import { AadAuthority } from './aadAuthority';
import { B2cAuthority } from './b2cAuthority';
import { Authority, AuthorityType } from './authority';
import { UrlUtils } from './urlUtils';

export class AuthorityFactory {
    /**
     * Parse the url and determine the type of authority
     */
    private static DetectAuthorityFromUrl(authorityUrl: string): AuthorityType {
        authorityUrl = UrlUtils.CanonicalizeUri(authorityUrl);
        const components = UrlUtils.GetUrlComponents(authorityUrl);
        const pathSegments = components.PathSegments;
        switch (pathSegments[0]) {
            case "tfp":
                return AuthorityType.B2C;
            default:
                return AuthorityType.Aad;
        }
    }

    /**
     * Create an authority object of the correct type based on the url
     * Performs basic authority validation - checks to see if the authority is of a valid type (eg aad, b2c)
     */
    public static CreateInstance(authorityUrl: string, validateAuthority: boolean): Authority | null {
        if (!authorityUrl) {
            return null;
        }

        const type = AuthorityFactory.DetectAuthorityFromUrl(authorityUrl);
        // Depending on above detection, create the right type.
        switch (type) {
            case AuthorityType.B2C:
                return new B2cAuthority(authorityUrl, validateAuthority);
            case AuthorityType.Aad:
                return new AadAuthority(authorityUrl, validateAuthority);
            default:
                throw new Error('Invalid authority type.');
        }
    }

}
