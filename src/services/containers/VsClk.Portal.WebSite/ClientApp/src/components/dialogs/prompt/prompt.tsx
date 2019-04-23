import React, { Component, ChangeEvent } from 'react';
import Modal from 'react-modal';

import './react-modal.css';

export interface PromptModalProps {
    open: boolean;
    placeholder?: string;
    onClose?: () => void;
    onPrompt: (value: string) => void;
}

export interface PromptModalState {
    opened?: boolean;
}

export class PromptModal extends Component<PromptModalProps, PromptModalState> {

    constructor(props: PromptModalProps) {
        super(props);
        this.state = {
            opened: props.open
        }
    }

    componentWillReceiveProps(newProps: PromptModalProps) {
        if (newProps.open !== this.props.open) {
            this.setState({ opened: newProps.open });
        }
    }

    onKeyDown = (e: React.KeyboardEvent) => {
        if (e.which === 13) {
            // complete prompt. 
            const value = (e.target as HTMLInputElement).value;
            const { onPrompt } = this.props;
            onPrompt(value);

            e.preventDefault();
            e.stopPropagation();
        }
    }

    onClose = () => {
        this.setState({ opened: false });

        const { onClose } = this.props;
        if (onClose) onClose();
    }

    render() {
        const { open, placeholder } = this.props;
        const { opened } = this.state;

        const modalStyles = {
            content: {
                backgroundColor: 'rgb(37, 37, 38)',
                boxShadow: 'rgb(0, 0, 0) 0px 5px 8px',
                top: '30px', width: '600px',
                marginLeft: '-300px'
            }
        };

        return (
            <Modal isOpen={opened} onRequestClose={this.onClose} style={modalStyles} overlayClassName={'vssass-code-modal'} className={'quick-input-widget'}
                shouldCloseOnEsc={true} shouldCloseOnOverlayClick={true} portalClassName={'monaco-workbench'}>
                <div className='quick-input-titlebar' style={{ backgroundColor: 'rgba(255, 255, 255, 0.106)' }}>
                    <div className='monaco-action-bar animated quick-input-left-action-bar'>
                        <ul className='actions-container' role='toolbar'>
                        </ul></div>
                    <div className='monaco-action-bar animated quick-input-right-action-bar'>
                        <ul className='actions-container' role='toolbar'></ul>
                    </div>
                </div>
                <div className='quick-input-header'>
                    <div className='quick-input-filter'>
                        <div className='quick-input-box'>
                            <div className='monaco-inputbox idle' style={{ backgroundColor: 'rgb(60, 60, 60)', color: 'rgb(204, 204, 204)' }}>
                                <div className='wrapper'>
                                    <input className='input' autoCorrect={'off'} tabIndex={0} autoFocus autoCapitalize='off' spellCheck={false} type='text' aria-describedby='quickInput_message'
                                        placeholder={placeholder} title={placeholder} style={{ backgroundColor: 'rgb(60, 60, 60)', color: 'rgb(204, 204, 204)' }} onKeyDown={this.onKeyDown}
                                        aria-haspopup='true' aria-autocomplete='list' aria-activedescendant='list_id_14_0' aria-label='Type to narrow down results.' />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </Modal >
        );
    }
}
