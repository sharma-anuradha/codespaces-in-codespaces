import { PersonaPresence } from 'office-ui-fabric-react/lib/Persona';

export interface IUser {
    name: string;
    imageUrl: string;
    presence: PersonaPresence;
}

export const users: IUser[] = [
    {
        name: 'Oleg Solomka',
        imageUrl: 'https://avatars1.githubusercontent.com/u/1478800?s=460',
        presence: PersonaPresence.online
    },
    {
        name: 'Peter Lisy',
        imageUrl: 'https://avatars2.githubusercontent.com/u/1951942?s=460',
        presence: PersonaPresence.busy
    },
    {
        name: 'Osvaldo Ortega',
        imageUrl: 'https://avatars1.githubusercontent.com/u/48293249?s=460',
        presence: PersonaPresence.away
    },
    {
        name: 'Jason Ginchereau',
        imageUrl: 'https://avatars0.githubusercontent.com/u/13093042?s=460',
        presence: PersonaPresence.away
    },
    {
        name: 'Nik Molnar',
        imageUrl: 'https://avatars3.githubusercontent.com/u/199026?s=460',
        presence: PersonaPresence.busy
    },
    {
        name: 'Srivatsn Narayanan',
        imageUrl: 'https://avatars3.githubusercontent.com/u/20570?s=460',
        presence: PersonaPresence.online
    },
    {
        name: 'Anthony van der Hoorn',
        imageUrl: 'https://avatars2.githubusercontent.com/u/585619?s=460',
        presence: PersonaPresence.away
    },
    {
        name: 'Jonathan Carter',
        imageUrl: 'https://avatars0.githubusercontent.com/u/116461?s=460',
        presence: PersonaPresence.online
    }
];