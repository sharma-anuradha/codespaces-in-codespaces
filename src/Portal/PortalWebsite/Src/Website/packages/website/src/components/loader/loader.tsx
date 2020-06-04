import React, { Component, Fragment } from 'react';
import { SpinnerLabelPosition } from 'office-ui-fabric-react/lib/Spinner';
import { isHostedOnGithub } from 'vso-client-core';
import { VsoLoader } from './VsoLoader';
import { TFunction } from 'i18next';

import './loader.css';

export interface LoaderProps {
    message?: string;
    labelPosition?: SpinnerLabelPosition;
    className?: string;
    translation: TFunction;
}

export class Loader extends Component<LoaderProps> {
    render() {
        return (!isHostedOnGithub())
            ? <VsoLoader {...this.props} />
            : <Fragment />;
    }
}
