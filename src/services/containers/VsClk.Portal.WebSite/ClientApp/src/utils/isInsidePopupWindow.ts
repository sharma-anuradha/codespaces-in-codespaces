
export const isInsidePopupWindow = () => {
    return window.opener && window.opener !== window;
};