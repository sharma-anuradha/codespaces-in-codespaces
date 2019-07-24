
export interface IMockedEnvironment {
    description: string;
    date: string;
}

export const environments: IMockedEnvironment[] = [
    {
        description: 'Front-end environment for the Node.js guestbook application',
        date: 'Tuesday, February 26 at 2:34pm'
    },
    {
        description: 'Back-end environment for working on our C#/.NET API services',
        date: 'Monday, March 06 at 6:03pm'
    },
    {
        description: 'Specialized GPU machine for training ML models',
        date: 'Wednesday, Juny 02 at 8:22am'
    },
];