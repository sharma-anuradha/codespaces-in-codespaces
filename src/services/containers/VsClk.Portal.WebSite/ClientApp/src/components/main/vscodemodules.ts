let initialCSSModules = [
    "vs/css!vs/workbench/contrib/welcome/page/browser/welcomePage",
    "vs/css!vs/workbench/browser/parts/titlebar/media/titlebarpart",
    "vs/css!vs/base/browser/ui/menu/menu",
    "vs/css!vs/workbench/browser/media/part",
    "vs/css!vs/workbench/browser/parts/statusbar/media/statusbarpart",
    "vs/css!vs/workbench/browser/parts/activitybar/media/activitybarpart",
    "vs/css!vs/base/browser/ui/actionbar/actionbar",
    "vs/css!vs/workbench/browser/parts/activitybar/media/activityaction",
    "vs/css!vs/workbench/browser/parts/panel/media/panelpart",
    "vs/css!vs/workbench/contrib/search/browser/media/search.contribution",
    "vs/css!vs/workbench/contrib/files/browser/media/explorerviewlet",
    "vs/css!vs/workbench/contrib/scm/browser/media/scmViewlet",
    "vs/css!vs/base/browser/ui/inputbox/inputBox",
    "vs/css!vs/workbench/browser/parts/quickinput/quickInput",
    "vs/css!vs/base/browser/ui/octiconLabel/octicons/octicons"
];
let lazyLoadModules = [
    "vs/editor/editor.all",

    "vs/workbench/browser/workbench.contribution",

    "vs/workbench/browser/actions/layoutActions",
    "vs/workbench/browser/actions/listCommands",
    "vs/workbench/browser/actions/navigationActions",
    "vs/workbench/browser/parts/quickopen/quickOpenActions",
    "vs/workbench/browser/parts/quickinput/quickInputActions",

    "vs/workbench/api/common/menusExtensionPoint",
    "vs/workbench/api/common/configurationExtensionPoint",
    "vs/workbench/api/browser/viewsExtensionPoint",


    "vs/platform/instantiation/common/extensions",
    "vs/platform/actions/common/actions",
    "vs/platform/actions/common/menuService",
    "vs/platform/list/browser/listService",
    "vs/editor/browser/services/openerService",
    "vs/platform/opener/common/opener",
    "vs/editor/common/services/editorWorkerService",
    "vs/editor/common/services/editorWorkerServiceImpl",
    "vs/editor/common/services/markerDecorationsServiceImpl",
    "vs/editor/common/services/markersDecorationService",
    "vs/platform/markers/common/markers",
    "vs/platform/markers/common/markerService",


    "vs/platform/contextkey/browser/contextKeyService",
    "vs/platform/contextkey/common/contextkey",
    "vs/editor/common/services/modelService",
    "vs/editor/common/services/modelServiceImpl",
    "vs/editor/common/services/resourceConfiguration",
    "vs/editor/common/services/resourceConfigurationImpl",
    "vs/platform/accessibility/common/accessibility",
    "vs/platform/accessibility/common/accessibilityService",
    "vs/platform/extensionManagement/common/extensionManagement",
    "vs/platform/extensionManagement/common/extensionEnablementService",
    "vs/platform/contextview/browser/contextView",
    "vs/platform/contextview/browser/contextMenuService",
    "vs/platform/contextview/browser/contextViewService",

    "vs/workbench/services/heap/common/heap",
    "vs/workbench/services/broadcast/common/broadcast",

    "vs/workbench/browser/nodeless.simpleservices",

    "vs/workbench/services/bulkEdit/browser/bulkEditService",
    "vs/workbench/services/keybinding/common/keybindingEditing",
    "vs/workbench/services/hash/common/hashService",
    "vs/workbench/services/configurationResolver/browser/configurationResolverService",
    "vs/workbench/services/decorations/browser/decorationsService",
    "vs/workbench/services/progress/browser/progressService2",
    "vs/workbench/services/editor/browser/codeEditorService",
    "vs/workbench/services/preferences/browser/preferencesService",
    "vs/workbench/services/output/common/outputChannelModelService",
    "vs/workbench/services/configuration/common/jsonEditingService",
    "vs/workbench/services/textmodelResolver/common/textModelResolverService",
    "vs/workbench/services/textfile/common/textFileService",
    "vs/workbench/services/dialogs/browser/fileDialogService",
    "vs/workbench/services/editor/browser/editorService",
    "vs/workbench/services/history/browser/history",
    "vs/workbench/services/activity/browser/activityService",
    "vs/workbench/browser/parts/views/views",
    "vs/workbench/services/untitled/common/untitledEditorService",
    "vs/workbench/services/mode/common/workbenchModeService",
    "vs/workbench/services/commands/common/commandService",
    "vs/workbench/services/themes/browser/workbenchThemeService",
    "vs/workbench/services/label/common/labelService",
    "vs/workbench/services/notification/common/notificationService",

    "vs/workbench/browser/parts/quickinput/quickInput",
    "vs/workbench/browser/parts/quickopen/quickOpenController",
    "vs/workbench/browser/parts/titlebar/titlebarPart",
    "vs/workbench/browser/parts/editor/editorPart",
    "vs/workbench/browser/parts/activitybar/activitybarPart",
    "vs/workbench/browser/parts/panel/panelPart",
    "vs/workbench/browser/parts/sidebar/sidebarPart",
    "vs/workbench/browser/parts/statusbar/statusbarPart",

    "vs/workbench/contrib/telemetry/browser/telemetry.contribution",

    "vs/workbench/contrib/preferences/browser/keybindingsEditorContribution",

    "vs/workbench/contrib/logs/common/logs.contribution",

    "vs/workbench/contrib/quickopen/browser/quickopen.contribution",

    "vs/workbench/contrib/files/browser/explorerViewlet",
    "vs/workbench/contrib/files/browser/fileActions.contribution",
    "vs/workbench/contrib/files/browser/files.contribution",

    "vs/workbench/contrib/backup/common/backup.contribution",

    "vs/workbench/contrib/search/browser/search.contribution",
    "vs/workbench/contrib/search/browser/searchView",
    "vs/workbench/contrib/search/browser/openAnythingHandler",

    "vs/workbench/contrib/scm/browser/scm.contribution",
    "vs/workbench/contrib/scm/browser/scmViewlet",

    "vs/workbench/contrib/markers/browser/markers.contribution",

    "vs/workbench/contrib/url/common/url.contribution",

    "vs/workbench/contrib/output/browser/output.contribution",
    "vs/workbench/contrib/output/browser/outputPanel",

    "vs/workbench/contrib/emmet/browser/emmet.contribution",

    "vs/workbench/contrib/codeEditor/browser/codeEditor.contribution",

    "vs/workbench/contrib/snippets/browser/snippets.contribution",
    "vs/workbench/contrib/snippets/browser/snippetsService",
    "vs/workbench/contrib/snippets/browser/insertSnippet",
    "vs/workbench/contrib/snippets/browser/configureSnippets",
    "vs/workbench/contrib/snippets/browser/tabCompletion",

    "vs/workbench/contrib/format/browser/format.contribution",

    "vs/workbench/contrib/themes/browser/themes.contribution",

    "vs/workbench/contrib/watermark/browser/watermark",

    "vs/workbench/contrib/welcome/walkThrough/browser/walkThrough.contribution",

    "vs/workbench/contrib/welcome/overlay/browser/welcomeOverlay",

    "vs/workbench/contrib/outline/browser/outline.contribution",

    "vs/nls!vs/workbench/workbench.nodeless.main",
    "vs/css!vs/workbench/workbench.nodeless.main"
];

export default {
    initialCSSModules,
    lazyLoadModules
}