import { updateFavicon } from '../../utils/updateFavicon';
import { DEFAULT_TITLE, FAVICON_PATH } from './constants';

export const initHostingHtmlTags = () => {
    document.title = DEFAULT_TITLE;
    updateFavicon(FAVICON_PATH);
};
