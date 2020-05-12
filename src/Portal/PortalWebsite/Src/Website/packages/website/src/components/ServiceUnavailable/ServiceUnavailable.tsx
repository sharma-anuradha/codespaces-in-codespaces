import React, { FC, useCallback, useState, FormEventHandler } from 'react';
import { useSelector } from 'react-redux';

import {
    Stack,
    StackItem,
    Text,
    TextField,
    PrimaryButton,
    ITextFieldProps,
    Label,
    MessageBar,
    MessageBarType,
} from 'office-ui-fabric-react';

import { PortalLayout } from '../portalLayout/portalLayout';
import { EverywhereImage } from '../EverywhereImage/EverywhereImage';
import { unavailableErrorMessage } from '../../actions/serviceUnavailable';
import { notificationsSubscribe } from '../../actions/notificationsSubscribe';
import { ApplicationState } from '../../reducers/rootReducer';
import { useDispatch } from '../../actions/middleware/useDispatch';

import './ServiceUnavailable.css';

const NoUserInfoForm: FC = () => null;
const FormWithEmail: FC<{ defaultUserEmail: string }> = ({ defaultUserEmail }) => {
    const [userEmail, setEmail] = useState(defaultUserEmail);
    const [disabled, setDisabled] = useState(false);
    const dispatch = useDispatch();
    const [showSuccessMessage, setShowSuccessMessage] = useState(false);
    const [showErrorMessage, setShowErrorMessage] = useState(false);

    const postEmail: FormEventHandler = useCallback(
        async (event) => {
            event.preventDefault();

            setDisabled(true);

            try {
                await dispatch(notificationsSubscribe(userEmail));
                setShowSuccessMessage(true);
            } catch {
                setShowErrorMessage(true);
                setDisabled(false);
            }
        },
        [userEmail]
    );

    const emailChangedCallback: ITextFieldProps['onChange'] = useCallback((_event, value = '') => {
        setEmail(value);
    }, []);

    return (
        <form onSubmit={postEmail}>
            <Stack horizontalAlign='center'>
                <Stack.Item>
                    <Label id='email-label' htmlFor='email'>
                        We can send you an email when we are ready to enable Codespace creation
                        again.
                    </Label>
                </Stack.Item>
                <Stack.Item>
                    <Stack horizontal>
                        <TextField
                            id='email'
                            type='email'
                            value={userEmail}
                            onChange={emailChangedCallback}
                            aria-labelledby='email-label'
                            className='email-form__text-field'
                        />
                        <PrimaryButton
                            type='submit'
                            className='email-form__button'
                            disabled={disabled}
                        >
                            Notify Me
                        </PrimaryButton>
                    </Stack>
                </Stack.Item>
                <Stack.Item>
                    {showSuccessMessage && (
                        <MessageBar messageBarType={MessageBarType.success} style={{ width: 358 }}>
                            We'll let you know.
                        </MessageBar>
                    )}
                </Stack.Item>
                <Stack.Item>
                    {showErrorMessage && (
                        <MessageBar messageBarType={MessageBarType.error} style={{ width: 358 }}>
                            Something went wrong. Please try again later.
                        </MessageBar>
                    )}
                </Stack.Item>
            </Stack>
        </form>
    );
};

const NotificationsForm: FC = () => {
    const { defaultUserEmail } = useSelector((state: ApplicationState) => ({
        defaultUserEmail: (state.userInfo && state.userInfo.mail) || '',
    }));

    if (!defaultUserEmail) {
        return <NoUserInfoForm />;
    }

    return <FormWithEmail defaultUserEmail={defaultUserEmail} />;
};

export const ServiceUnavailable: FC = () => {
    return (
        <PortalLayout hideNavigation>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                tokens={{ childrenGap: 'l1' }}
                className='service-unavailable-page'
            >
                <Stack.Item>
                    <Text className='service-unavailable-page__title'>Visual Studio Codespaces</Text>
                </Stack.Item>

                <StackItem>
                    <EverywhereImage />
                </StackItem>

                <Stack.Item className='service-unavailable-page__message'>
                    {unavailableErrorMessage}
                </Stack.Item>
                <Stack.Item>
                    <NotificationsForm />
                </Stack.Item>
            </Stack>
        </PortalLayout>
    );
};
