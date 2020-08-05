import * as React from 'react';

import { TEnvironment } from '../../../config/config';

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

export const DevPanelHeaderGitInfo: React.FunctionComponent<{}> = () => {
    const gitBranch = process.env.VSCS_GIT_BRANCH;
    const gitSHA = process.env.VSCS_GIT_SHA;

    if (!gitBranch || !gitSHA) {
        return null;
    }

    const shortSha = gitSHA.substr(0, 7);

    return (
        <span
            className='vscs-dev-panel-header__section vscs-dev-panel-header__git-info'
            title={`${gitBranch} • ${shortSha}`}
        >
            <span className='vscs-dev-panel-header__section-title'>git:</span>
            <p className='vscs-dev-panel-header__section-value'>
                {gitBranch}{' '}
                <a className='vscs-dev-panel-header__section-value' href='#'>
                    {shortSha}
                </a>
            </p>
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
    return (
        <div className='vscs-dev-panel-header vscs-dev-panel__header' onClick={onClick}>
            <span
                className='vscs-dev-panel-header__section vscs-dev-panel-header__emoji'
                title={environment}
            >
                {envToEmoji(environment)}
            </span>
            <span className='vscs-dev-panel-header__section'>
                <span className='vscs-dev-panel-header__section-title'>env:</span>
                <p className='vscs-dev-panel-header__section-value'>{environment}</p>
            </span>
            <DevPanelHeaderGitInfo />
        </div>
    );
};
