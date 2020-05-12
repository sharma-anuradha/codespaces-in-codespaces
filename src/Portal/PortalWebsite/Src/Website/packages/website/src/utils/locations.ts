export function locationToDisplayName(location: string) {
    switch (location) {
        case 'EastUs':
            return 'East US';
        case 'SouthEastAsia':
            return 'Southeast Asia';
        case 'WestEurope':
            return 'West Europe';
        case 'WestUs2':
            return 'West US 2';

        default:
            return location;
    }
}