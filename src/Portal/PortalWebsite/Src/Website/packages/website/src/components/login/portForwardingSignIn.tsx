import React, { useRef, useEffect } from 'react';
import { useSelector } from 'react-redux';
import { RouteComponentProps, Redirect } from 'react-router-dom';

import { ApplicationState } from '../../reducers/rootReducer';
import { PortalLayout } from '../portalLayout/portalLayout';
import './portForwardingSignIn.css';

type SignInProps = RouteComponentProps<{ environmentId: string }>;

export const PortFowardingSingInPage: React.SFC<SignInProps> = (props) => {
    var formRef = useRef<HTMLFormElement>(null);

    const environmentId = props.match.params.environmentId;
    const query = props.location.search;
    const portString = new URLSearchParams(props.location.search).get('port');

    const { token, portForwardingDomainTemplate } = useSelector((state: ApplicationState) => {
        return {
            token: state.authentication.token,
            portForwardingDomainTemplate: state.configuration?.portForwardingDomainTemplate,
        };
    });

    const postUrl = new URL(
        `/authenticate-codespace/${environmentId}${query}`,
        `https://${portForwardingDomainTemplate?.replace('{0}', `${environmentId}-${portString}`)}`
    );

    useEffect(() => {
        if (token && formRef.current) {
            formRef.current.submit();
        }
    }, [token]);

    if (!portString) {
        return <Redirect to={`/environment/${environmentId}`} />;
    }

    return (
        <PortalLayout hideNavigation>
            <p className='vsonline-port-forwarding__status'>Connecting to the forwarded port...</p>

            <form action={postUrl.href} method='POST' ref={formRef}>
                <input type='hidden' name='token' value={token} />
            </form>
        </PortalLayout>
    );
};
