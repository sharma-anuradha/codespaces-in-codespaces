// TrafficManagerValues.ts

import { IRegionNames } from "./ResourceNameDefs";

export interface ITrafficManagerEndpointValues {
    trafficManagerEndpointGeoMappings: string;
}

const geoMappings: { [key: string]: string } = {
    // dev-ci
    'dev-ci-eu-w': 'GEO-EU GEO-ME GEO-AF GEO-AN',
    'dev-ci-us-w2': 'WORLD GEO-NA GEO-AS GEO-SA GEO-AP',
    // ppe-load
    'ppe-load-ap-se': 'GEO-AS GEO-AP',
    'ppe-load-eu-w': 'GEO-EU GEO-ME GEO-AF GEO-AN',
    'ppe-load-us-e': 'GEO-SA',
    'ppe-load-us-w2': 'WORLD GEO-NA',
    // ppe-rel
    'ppe-rel-ap-se': 'GEO-AS GEO-AP GEO-ME GEO-AF',
    'ppe-rel-us-w2': 'WORLD GEO-NA GEO-SA GEO-EU GEO-AN',
    // prod-can
    'prod-can-usec': 'WORLD',
    // prod-rel
    'prod-rel-ap-se': 'GEO-AS GEO-AP',
    'prod-rel-eu-w': 'GEO-EU GEO-ME GEO-AF GEO-AN',
    'prod-rel-us-e': 'GEO-SA GEO-NA CA-NB CA-NL CA-NS CA-ON CA-PE CA-QC US-AL US-AR US-CT US-DC US-DE US-FL US-GA US-IA US-IL US-IN US-KY US-LA US-MA US-MD US-ME US-MI US-MN US-MO US-MS US-NC US-NH US-NJ US-NY US-OH US-PA US-RI US-SC US-TN US-VA US-VT US-WI US-WV',
    'prod-rel-us-w2': 'WORLD UM US CA CA-AB CA-BC CA-MB CA-NT CA-NU CA-SK CA-YT US-AK US-AZ US-CA US-CO US-HI US-ID US-KS US-MT US-ND US-NE US-NM US-NV US-OK US-OR US-SD US-TX US-UT US-WA US-WY',
};

export abstract class TrafficManagerValues {

    public static assignValues(region: IRegionNames): ITrafficManagerEndpointValues {

        if (region.plane === 'ctl') {
            if (region.component === 'codesp') {
                const geoKey = `${region.env}-${region.instance}-${region.region}`;
                const endpointGeoMappings = geoMappings[geoKey] || 'WORLD';
                const values: ITrafficManagerEndpointValues = {
                    trafficManagerEndpointGeoMappings: endpointGeoMappings,
                };
                Object.assign(region, values);
                return values;
            }
        }

        return null;
    }

}