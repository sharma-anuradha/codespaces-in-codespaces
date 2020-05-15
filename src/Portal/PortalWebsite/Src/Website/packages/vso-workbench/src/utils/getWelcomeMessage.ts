import { TEnvironment } from '../config/config';

const CAPTION = ' * VS Codespaces *  ';
const SEPARATOR_MIDDLE = new Array(CAPTION.length)
    .fill('-')
    .join('');

const SEPARATOR = `+${SEPARATOR_MIDDLE}+`

const padString = (str: string, max = SEPARATOR.length - 4) => {
    const delta = max - str.length;
    const padEnd = Math.ceil(delta / 2);
    const padSymbol = ' ';

    const result = str
        .padStart(max - padEnd, padSymbol)
        .padEnd(max, padSymbol);

    return result;
};

export const getWelcomeMessage = (environment: TEnvironment, version: string) => {
    const block = [
        `|${CAPTION}|`,
        `| ${padString(version)} |`,
    ];

    if (environment !== 'production') {
        block.push(`| ${padString(environment)} |`);
    }

    return [
        SEPARATOR,
        block.join(`\n${SEPARATOR}\n`),
        SEPARATOR,
    ].join('\n');
};