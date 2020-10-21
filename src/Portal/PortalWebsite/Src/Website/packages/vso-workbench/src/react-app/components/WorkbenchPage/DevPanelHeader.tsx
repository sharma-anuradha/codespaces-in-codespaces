import * as React from 'react';

import { TEnvironment } from '../../../config/config';

import { DevPanelHeaderValue } from './DevPanelHeaderValue';
import { DevPanelHeaderPerformance } from './DevPanelHeaderPerformance';

import './DevPanelHeader.css';

export const LOADING_ENVIRONMENT_STAGE = 'loading...';

const envToEmoji = (env: IDevPanelHeaderProps['environment']) => {
    switch (env) {
        case LOADING_ENVIRONMENT_STAGE: {
            return '⌛';
        }
        case 'local': {
            return '🧑‍💻';
        }
        case 'development': {
            return '🚧';
        }
        case 'staging': {
            return '🏗';
        }
        case 'production': {
            return '📣';
        }
    }
};

const envToPrettyName = (env: IDevPanelHeaderProps['environment']) => {
    switch (env) {
        case 'staging': {
            return 'pre-production';
        }
        default: {
            return env;
        }
    }
};

export const DevPanelHeaderGitInfo: React.FunctionComponent<{}> = () => {
    const gitBranch = process.env.VSCS_GIT_BRANCH;
    const gitSHA = process.env.VSCS_GIT_SHA;

    if (!gitBranch || !gitSHA) {
        return null;
    }

    const shortSha = gitSHA.substr(0, 7);

    return (
        <span
            className='vscs-dev-panel-header__section vscs-dev-panel-header__section--text vscs-dev-panel-header__git-info'
            title={`${gitBranch} • ${shortSha}`}
        >
            <span className='vscs-dev-panel-header__section-title'>git:</span>
            <DevPanelHeaderValue text={`${gitBranch}`}>
                <DevPanelHeaderValue>
                    <a className='vscs-dev-panel-header__section-value' href='#'>
                        {shortSha}
                    </a>
                </DevPanelHeaderValue>
            </DevPanelHeaderValue>
        </span>
    );
};

interface IDevPanelHeaderEmojiProps {
    title: string;
    emoji: string;
}

export const DevPanelHeaderEmoji: React.FunctionComponent<IDevPanelHeaderEmojiProps> = (
    props: IDevPanelHeaderEmojiProps
) => {
    const { title, emoji } = props;
    return (
        <span title={title} className='vscs-dev-panel-header__section vscs-dev-panel-header__emoji'>
            {emoji}
        </span>
    );
};

interface IDevPanelHeaderProps {
    environment: TEnvironment | typeof LOADING_ENVIRONMENT_STAGE;
    onClick: (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => void;
}

export const DevPanelHeader: React.FunctionComponent<IDevPanelHeaderProps> = (
    props: IDevPanelHeaderProps
) => {
    const { environment, onClick } = props;
    const env = envToPrettyName(environment);

    const codespaceEmoji =
        process.env.VSCS_IN_CODESPACE === 'true' ? (
            <DevPanelHeaderEmoji emoji='🚀' title='in a Codespace' />
        ) : null;

    return (
        <div className='vscs-dev-panel-header vscs-dev-panel__header' onMouseUp={onClick}>
            <DevPanelHeaderEmoji emoji={envToEmoji(environment)} title={`env: ${env}`} />
            <DevPanelHeaderPerformance />

            {codespaceEmoji}

            <span className='vscs-dev-panel-header__section vscs-dev-panel-header__section--text'>
                <span className='vscs-dev-panel-header__section-title'>env:</span>
                <DevPanelHeaderValue text={env} />
            </span>
            <DevPanelHeaderGitInfo />
        </div>
    );
};

