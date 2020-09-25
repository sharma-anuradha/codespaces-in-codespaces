import React, { useRef, useEffect } from 'react';
import { useSelector } from 'react-redux';
import { RouteComponentProps, Redirect } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { PortalLayout } from '../portalLayout/portalLayout';
import './portForwardingSignIn.css';

type SignInProps = RouteComponentProps<{ environmentId: string }>;

export const PortForwardingSingInPage: React.FunctionComponent<SignInProps> = (props) => {
    var formRef = useRef<HTMLFormElement>(null);

    const environmentId = props.match.params.environmentId;
    const query = props.location.search;
    const portString = new URLSearchParams(props.location.search).get('port');

    const { token, portForwardingDomainTemplate, portForwardingServiceEnabled } = useSelector(
            (state: ApplicationState) => {
                return {
                    token: state.authentication.token,
                    portForwardingServiceEnabled: state.configuration?.portForwardingServiceEnabled,
                    portForwardingDomainTemplate: state.configuration?.portForwardingDomainTemplate,
                };
            }
        );

    const postUrl = new URL(
        `/authenticate-codespace/${environmentId}${query}`,
        `https://${portForwardingDomainTemplate?.replace('{0}', `${environmentId}-${portString}`)}`
    );

    useEffect(() => {
        if (token && formRef.current) {
            formRef.current.submit();
        }
    }, [token]);

    const parsedPort =
        portString != null && portString.length > 0 ? Number.parseInt(portString, 10) : -1;

    if (
        parsedPort < 0 ||
        parsedPort >= 65535 ||
        isNaN(parsedPort) ||
        // Number.parseInt will trim non-number trailing characters.
        parsedPort.toString().length != portString?.length
    ) {
        return <Redirect to={`/environment/${environmentId}`} />;
    }

    const featureFlags = JSON.stringify({
        portForwardingServiceEnabled,
    });

    return (
        <PortalLayout hideNavigation>
            <p className='vsonline-port-forwarding__status'>Connecting to the forwarded port...</p>

            <form action={postUrl.href} method='POST' ref={formRef}>
                <input type='hidden' name='token' value={token} />
                <input type='hidden' name='featureFlags' value={featureFlags} />
            </form>
        </PortalLayout>
    );
};
