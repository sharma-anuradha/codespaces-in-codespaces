import { isHostedOnGithub } from './utils/isHostedOnGithub';

import { initHostingHtmlTags as initHostingHtmlTagsVSO } from './split/vso/initHostingHtmlTags';
import { initHostingHtmlTags as initHostingHtmlTagsGithub } from './split/github/initHostingHtmlTagsGithub';

export const initHostingHtmlTags = async () => {
    (!isHostedOnGithub())
        ? await initHostingHtmlTagsVSO()
        : await initHostingHtmlTagsGithub();
};
