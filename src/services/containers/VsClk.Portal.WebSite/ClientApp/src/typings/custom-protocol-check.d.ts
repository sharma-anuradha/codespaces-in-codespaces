declare module 'custom-protocol-check' {
    type Callback = () => void;

    export default function(
        uri: string,
        failCb: Callback | undefined,
        successCb: Callback | undefined,
        unsupportedCb: Callback
    );
}
