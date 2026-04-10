/// <loc filename="Metadata\ui.strings_loc_oam.xml" format="messagebundle" />
/*!
  © Microsoft. All rights reserved.

  This library is supported for use in Windows Store apps only.

  Build: 1.0.9200.20789.win8_ldr.130802-2151
  
  Version: Microsoft.WinJS.1.0
*/

(function (global) {
    global.strings = global.strings || {};

    var appxVersion = "Microsoft.WinJS.1.0";
    var developerPrefix = "Developer.";
    if (appxVersion.indexOf(developerPrefix) === 0) {
        appxVersion = appxVersion.substring(developerPrefix.length);
    }

    function addStrings(keyPrefix,  strings) {
        Object.keys(strings).forEach(function (key) {
            global.strings[keyPrefix + key.replace("\\", "/")] = strings[key];
        });
    }
    
addStrings(
"ms-resource://"+appxVersion+"/ui/",

{
    "appBarAriaLabel": "App Bar",
    "appBarCommandAriaLabel": "App Bar Item",
    "automaticallyLoadPagesIsInvalid": "Invalid argument: automaticallyLoadPages must be a boolean.",
    "averageRating": "Average Rating",
    "backbuttonarialabel": "Back",
    "badAlignment": "Invalid argument: Flyout alignment should be 'center' (default), 'left', or 'right'.",
    "badAxis": "Invalid argument: orientation must be a string, either 'horizontal' or 'vertical'",
    "badButtonElement": "Invalid argument: For a button, toggle, or flyout command, the element must be null or a button element",
    "badClick": "Invalid argument: The onclick property for an {0} must be a function",
    "badCurrentPage": "Invalid argument: currentPage must be a number greater than or equal to zero and be within the bounds of the datasource",
    "badFlyout": "Invalid argument: The flyout property for an {0} must be a Flyout or String id of a Flyout",
    "badHrElement": "Invalid argument: For a separator, the element must be null or an hr element",
    "badItemSpacingAmount": "Invalid argument: itemSpacing must be a number greater than or equal to zero",
    "badLayout": "Invalid argument: The layout property must be 'custom' or 'commands'",
    "badPlacement": "Invalid argument: Flyout placement should be 'top' (default), 'bottom', 'left', 'right', or 'auto'.",
    "badReference": "Invalid argument: Invalid href to settings flyout fragment",
    "cannotChangeCommandsWhenVisible": "Invalid argument: You must call hide() before changing {0} commands",
    "cannotChangeHiddenProperty": "Unable to set hidden property while parent {0} is visible.",
    "cannotChangeLayoutWhenVisible": "Invalid argument: The layout property cannot be set when the AppBar is visible, call hide() first",
    "cannotChangePlacementWhenVisible": "Invalid argument: The placement property cannot be set when the AppBar is visible, call hide() first",
    "clearYourRating" : "Clear your rating",
    "closeOverlay" : "Close",
    "duplicateConstruction": "Invalid argument: Controls may only be instantiated one time for each DOM element",
    "flipViewNavigationDuringStateChange": "Error: After changing itemDataSource or itemTemplate, any navigation in the FlipView control should be delayed until the pageselected event is fired.",
    "flipViewPanningContainerAriaLabel": "Scrolling Container",
    "flyoutAriaLabel": "Flyout",
    "indexIsInvalid": "Invalid argument: index must be a non-negative integer.",
    "invalidCountReturned": "Error: data adapter should return undefined, null, CountResult.unknown, or a non-negative integer for the count.",
    "invalidIndexReturned": "Error: data adapter should return undefined, null or a non-negative integer for the index.",
    "invalidItemReturned": "Error: data adapter returned item that is not an object.",
    "invalidItemsManagerCallback": "Invalid argument: {0} must be a function.",
    "invalidKeyReturned": "Error: data adapter returned item with undefined or null key.",
    "invalidViewBoxChildren": "ViewBox expects to only have one child element",
    "invalidTemplate": "Invalid template: Templates must be created before being passed to the ListView, and must contain a valid tree of elements.",
    "invalidZoomFactor": "Invalid zoomFactor",
    "invalidRequestedCountReturned": "Error: data adapter should return CountResult.unknown, CountResult.failure, or a non-negative integer for the count.",
    "itemIsInvalid": "Invalid argument: item must be a DOM element that was returned by the Items Manager, and has not been replaced or released.",
    "itemRendererIsInvalid": "Invalid argument: itemRenderer must be a function.",
    "keyIsInvalid": "Invalid argument: key must be a string.",
    "layoutIsInvalid": "Invalid argument: layout must be one of following values: 'verticalgrid', 'horizontalgrid' or 'list'.",
    "layoutNotInitialized": "Layout is not initialized.",
    "listDataAdapterIsInvalid": "Invalid argument: listDataAdapter must be an object or an array.",
    "listDataSourceIsInvalid": "Invalid argument: dataSource must be an object.",
    "listViewInvalidItem": "Item must provide index, key or description of corresponding item.",
    "listViewViewportAriaLabel": "Scrolling Container",
    "loadingBehaviorIsInvalid": "Invalid argument: loadingBehavior must be 'incremental' or 'randomAccess'.",
    "menuCommandAriaLabel": "Menu Item",
    "menuAriaLabel": "Menu",
    "modeIsInvalid": "Invalid argument: mode must be one of following values: 'none', 'single' or 'multi'.",
    "mustContainCommands": "Invalid HTML: AppBars/Menus must contain only AppBarCommands/MenuCommands",
    "noAnchor": "Invalid argument: Showing flyout requires a DOM element as its parameter.",
    "noitemsManagerForCount": "Invalid operation: can't get count if no dataSource has been set",
    "notCompatibleWithSemanticZoom": "ListView can only be used with SemanticZoom if randomAccess loading behavior is specified.",
    "off" : "Off",
    "on" : "On",
    "pagesToLoadIsInvalid": "Invalid argument: pagesToLoad must be a positive number.",
    "pagesToLoadThresholdIsInvalid": "Invalid argument: pagesToLoadThreshold must be a positive number.",
    "requiresCommands": "Invalid argument: commands must not be empty",
    "selectAMPM": "Select A.M P.M",
    "selectDay": "Select Day",
    "selectHour": "Select Hour",
    "selectMinute": "Select Minute",
    "selectMonth": "Select Month",
    "selectYear": "Select Year",
    "settingsFlyoutAriaLabel": "Settings Flyout",
    "tentativeRating": "Tentative Rating",
    "tooltipStringsIsInvalid": "Invalid argument: tooltipStrings must be null or an array of strings.",
    "unrated": "Unrated",
    "userRating": "User Rating",
    // AppBar Icons follow, the format of the ui.js and ui.resjson differ for
    // the AppBarIcon namespace.  The remainder of the file therefore differs.
    // Code point comments are the icon glyphs in the 'Segoe UI Symbol' font.
    "appBarIcons\\previous":           "\uE100", //  group:Media
    "appBarIcons\\next":               "\uE101", //  group:Media
    "appBarIcons\\play":               "\uE102", //  group:Media
    "appBarIcons\\pause":              "\uE103", //  group:Media
    "appBarIcons\\edit":               "\uE104", //  group:File
    "appBarIcons\\save":               "\uE105", //  group:File
    "appBarIcons\\clear":              "\uE106", //  group:File
    "appBarIcons\\delete":             "\uE107", //  group:File
    "appBarIcons\\remove":             "\uE108", //  group:File
    "appBarIcons\\add":                "\uE109", //  group:File
    "appBarIcons\\cancel":             "\uE10A", //  group:Editing
    "appBarIcons\\accept":             "\uE10B", //  group:General
    "appBarIcons\\more":               "\uE10C", //  group:General
    "appBarIcons\\redo":               "\uE10D", //  group:Editing
    "appBarIcons\\undo":               "\uE10E", //  group:Editing
    "appBarIcons\\home":               "\uE10F", //  group:General
    "appBarIcons\\up":                 "\uE110", //  group:General
    "appBarIcons\\forward":            "\uE111", //  group:General
    "appBarIcons\\right":              "\uE111", //  group:General
    "appBarIcons\\back":               "\uE112", //  group:General
    "appBarIcons\\left":               "\uE112", //  group:General
    "appBarIcons\\favorite":           "\uE113", //  group:Media
    "appBarIcons\\camera":             "\uE114", //  group:System
    "appBarIcons\\settings":           "\uE115", //  group:System
    "appBarIcons\\video":              "\uE116", //  group:Media
    "appBarIcons\\sync":               "\uE117", //  group:Media
    "appBarIcons\\download":           "\uE118", //  group:Media
    "appBarIcons\\mail":               "\uE119", //  group:Mail and calendar
    "appBarIcons\\find":               "\uE11A", //  group:Data
    "appBarIcons\\help":               "\uE11B", //  group:General
    "appBarIcons\\upload":             "\uE11C", //  group:Media
    "appBarIcons\\emoji":              "\uE11D", //  group:Communications
    "appBarIcons\\twopage":            "\uE11E", //  group:Layout
    "appBarIcons\\leavechat":          "\uE11F", //  group:Communications
    "appBarIcons\\mailforward":        "\uE120", //  group:Mail and calendar
    "appBarIcons\\clock":              "\uE121", //  group:General
    "appBarIcons\\send":               "\uE122", //  group:Mail and calendar
    "appBarIcons\\crop":               "\uE123", //  group:Editing
    "appBarIcons\\rotatecamera":       "\uE124", //  group:System
    "appBarIcons\\people":             "\uE125", //  group:Communications
    "appBarIcons\\closepane":          "\uE126", //  group:Layout
    "appBarIcons\\openpane":           "\uE127", //  group:Layout
    "appBarIcons\\world":              "\uE128", //  group:General
    "appBarIcons\\flag":               "\uE129", //  group:Mail and calendar
    "appBarIcons\\previewlink":        "\uE12A", //  group:General
    "appBarIcons\\globe":              "\uE12B", //  group:Communications
    "appBarIcons\\trim":               "\uE12C", //  group:Editing
    "appBarIcons\\attachcamera":       "\uE12D", //  group:System
    "appBarIcons\\zoomin":             "\uE12E", //  group:Layout
    "appBarIcons\\bookmarks":          "\uE12F", //  group:Editing
    "appBarIcons\\document":           "\uE130", //  group:File
    "appBarIcons\\protecteddocument":  "\uE131", //  group:File
    "appBarIcons\\page":               "\uE132", //  group:Layout
    "appBarIcons\\bullets":            "\uE133", //  group:Editing
    "appBarIcons\\comment":            "\uE134", //  group:Communications
    "appBarIcons\\mail2":              "\uE135", //  group:Mail and calendar
    "appBarIcons\\contactinfo":        "\uE136", //  group:Communications
    "appBarIcons\\hangup":             "\uE137", //  group:Communications
    "appBarIcons\\viewall":            "\uE138", //  group:Data
    "appBarIcons\\mappin":             "\uE139", //  group:General
    "appBarIcons\\phone":              "\uE13A", //  group:Communications
    "appBarIcons\\videochat":          "\uE13B", //  group:Communications
    "appBarIcons\\switch":             "\uE13C", //  group:Communications
    "appBarIcons\\contact":            "\uE13D", //  group:Communications
    "appBarIcons\\rename":             "\uE13E", //  group:File
    "appBarIcons\\pin":                "\uE141", //  group:System
    "appBarIcons\\musicinfo":          "\uE142", //  group:Media
    "appBarIcons\\go":                 "\uE143", //  group:General
    "appBarIcons\\keyboard":           "\uE144", //  group:System
    "appBarIcons\\dockleft":           "\uE145", //  group:Layout
    "appBarIcons\\dockright":          "\uE146", //  group:Layout
    "appBarIcons\\dockbottom":         "\uE147", //  group:Layout
    "appBarIcons\\remote":             "\uE148", //  group:System
    "appBarIcons\\refresh":            "\uE149", //  group:Data
    "appBarIcons\\rotate":             "\uE14A", //  group:Layout
    "appBarIcons\\shuffle":            "\uE14B", //  group:Media
    "appBarIcons\\list":               "\uE14C", //  group:Editing
    "appBarIcons\\shop":               "\uE14D", //  group:General
    "appBarIcons\\selectall":          "\uE14E", //  group:Data
    "appBarIcons\\orientation":        "\uE14F", //  group:Layout
    "appBarIcons\\import":             "\uE150", //  group:Data
    "appBarIcons\\importall":          "\uE151", //  group:Data
    "appBarIcons\\browsephotos":       "\uE155", //  group:Media
    "appBarIcons\\webcam":             "\uE156", //  group:System
    "appBarIcons\\pictures":           "\uE158", //  group:Media
    "appBarIcons\\savelocal":          "\uE159", //  group:File
    "appBarIcons\\caption":            "\uE15A", //  group:Media
    "appBarIcons\\stop":               "\uE15B", //  group:Media
    "appBarIcons\\showresults":        "\uE15C", //  group:Data
    "appBarIcons\\volume":             "\uE15D", //  group:Media
    "appBarIcons\\repair":             "\uE15E", //  group:System
    "appBarIcons\\message":            "\uE15F", //  group:Communications
    "appBarIcons\\page2":              "\uE160", //  group:Layout
    "appBarIcons\\calendarday":        "\uE161", //  group:Mail and calendar
    "appBarIcons\\calendarweek":       "\uE162", //  group:Mail and calendar
    "appBarIcons\\calendar":           "\uE163", //  group:Mail and calendar
    "appBarIcons\\characters":         "\uE164", //  group:Editing
    "appBarIcons\\mailreplyall":       "\uE165", //  group:Mail and calendar
    "appBarIcons\\read":               "\uE166", //  group:Mail and calendar
    "appBarIcons\\link":               "\uE167", //  group:Communications
    "appBarIcons\\accounts":           "\uE168", //  group:Communications
    "appBarIcons\\showbcc":            "\uE169", //  group:Mail and calendar
    "appBarIcons\\hidebcc":            "\uE16A", //  group:Mail and calendar
    "appBarIcons\\cut":                "\uE16B", //  group:Editing
    "appBarIcons\\attach":             "\uE16C", //  group:Mail and calendar
    "appBarIcons\\paste":              "\uE16D", //  group:Editing
    "appBarIcons\\filter":             "\uE16E", //  group:Data
    "appBarIcons\\copy":               "\uE16F", //  group:Editing
    "appBarIcons\\emoji2":             "\uE170", //  group:Mail and calendar
    "appBarIcons\\important":          "\uE171", //  group:Mail and calendar
    "appBarIcons\\mailreply":          "\uE172", //  group:Mail and calendar
    "appBarIcons\\slideshow":          "\uE173", //  group:Media
    "appBarIcons\\sort":               "\uE174", //  group:Data
    "appBarIcons\\manage":             "\uE178", //  group:System
    "appBarIcons\\allapps":            "\uE179", //  group:System
    "appBarIcons\\disconnectdrive":    "\uE17A", //  group:System
    "appBarIcons\\mapdrive":           "\uE17B", //  group:System
    "appBarIcons\\newwindow":          "\uE17C", //  group:System
    "appBarIcons\\openwith":           "\uE17D", //  group:System
    "appBarIcons\\contactpresence":    "\uE181", //  group:Communications
    "appBarIcons\\priority":           "\uE182", //  group:Mail and calendar
    "appBarIcons\\uploadskydrive":     "\uE183", //  group:File
    "appBarIcons\\gototoday":          "\uE184", //  group:Mail and calendar
    "appBarIcons\\font":               "\uE185", //  group:Editing
    "appBarIcons\\fontcolor":          "\uE186", //  group:Editing
    "appBarIcons\\contact2":           "\uE187", //  group:Communications
    "appBarIcons\\folder":             "\uE188", //  group:File
    "appBarIcons\\audio":              "\uE189", //  group:Media
    "appBarIcons\\placeholder":        "\uE18A", //  group:General
    "appBarIcons\\view":               "\uE18B", //  group:Layout
    "appBarIcons\\setlockscreen":      "\uE18C", //  group:System
    "appBarIcons\\settile":            "\uE18D", //  group:System
    "appBarIcons\\cc":                 "\uE190", //  group:Media
    "appBarIcons\\stopslideshow":      "\uE191", //  group:Media
    "appBarIcons\\permissions":        "\uE192", //  group:System
    "appBarIcons\\highlight":          "\uE193", //  group:Editing
    "appBarIcons\\disableupdates":     "\uE194", //  group:System
    "appBarIcons\\unfavorite":         "\uE195", //  group:Media
    "appBarIcons\\unpin":              "\uE196", //  group:System
    "appBarIcons\\openlocal":          "\uE197", //  group:File
    "appBarIcons\\mute":               "\uE198", //  group:Media
    "appBarIcons\\italic":             "\uE199", //  group:Editing
    "appBarIcons\\underline":          "\uE19A", //  group:Editing
    "appBarIcons\\bold":               "\uE19B", //  group:Editing
    "appBarIcons\\movetofolder":       "\uE19C", //  group:File
    "appBarIcons\\likedislike":        "\uE19D", //  group:Data
    "appBarIcons\\dislike":            "\uE19E", //  group:Data
    "appBarIcons\\like":               "\uE19F", //  group:Data
    "appBarIcons\\alignright":         "\uE1A0", //  group:Editing
    "appBarIcons\\aligncenter":        "\uE1A1", //  group:Editing
    "appBarIcons\\alignleft":          "\uE1A2", //  group:Editing
    "appBarIcons\\zoom":               "\uE1A3", //  group:Layout
    "appBarIcons\\zoomout":            "\uE1A4", //  group:Layout
    "appBarIcons\\openfile":           "\uE1A5", //  group:File
    "appBarIcons\\otheruser":          "\uE1A6", //  group:System
    "appBarIcons\\admin":              "\uE1A7", //  group:System
    "appBarIcons\\street":             "\uE1C3", //  group:General
    "appBarIcons\\map":                "\uE1C4", //  group:General
    "appBarIcons\\clearselection":     "\uE1C5", //  group:Data
    "appBarIcons\\fontdecrease":       "\uE1C6", //  group:Editing
    "appBarIcons\\fontincrease":       "\uE1C7", //  group:Editing
    "appBarIcons\\fontsize":           "\uE1C8", //  group:Editing
    "appBarIcons\\cellphone":          "\uE1C9", //  group:Communications
    "appBarIcons\\reshare":            "\uE1CA", //  group:Communications
    "appBarIcons\\tag":                "\uE1CB", //  group:Data
    "appBarIcons\\repeatone":          "\uE1CC", //  group:Media
    "appBarIcons\\repeatall":          "\uE1CD", //  group:Media
    "appBarIcons\\outlinestar":        "\uE1CE", //  group:Data
    "appBarIcons\\solidstar":          "\uE1CF", //  group:Data
    "appBarIcons\\calculator":         "\uE1D0", //  group:General
    "appBarIcons\\directions":         "\uE1D1", //  group:General
    "appBarIcons\\target":             "\uE1D2", //  group:General
    "appBarIcons\\library":            "\uE1D3", //  group:Media
    "appBarIcons\\phonebook":          "\uE1D4", //  group:Communications
    "appBarIcons\\memo":               "\uE1D5", //  group:Communications
    "appBarIcons\\microphone":         "\uE1D6", //  group:System
    "appBarIcons\\postupdate":         "\uE1D7", //  group:Communications
    "appBarIcons\\backtowindow":       "\uE1D8", //  group:Layout
    "appBarIcons\\fullscreen":         "\uE1D9", //  group:Layout
    "appBarIcons\\newfolder":          "\uE1DA", //  group:File
    "appBarIcons\\calendarreply":      "\uE1DB", //  group:Mail and calendar
    "appBarIcons\\unsyncfolder":       "\uE1DD", //  group:File
    "appBarIcons\\reporthacked":       "\uE1DE", //  group:Communications
    "appBarIcons\\syncfolder":         "\uE1DF", //  group:File
    "appBarIcons\\blockcontact":       "\uE1E0", //  group:Communications
    "appBarIcons\\switchapps":         "\uE1E1", //  group:System
    "appBarIcons\\addfriend":          "\uE1E2", //  group:Communications
    "appBarIcons\\touchpointer":       "\uE1E3", //  group:System
    "appBarIcons\\gotostart":          "\uE1E4", //  group:System
    "appBarIcons\\zerobars":           "\uE1E5", //  group:System
    "appBarIcons\\onebar":             "\uE1E6", //  group:System
    "appBarIcons\\twobars":            "\uE1E7", //  group:System
    "appBarIcons\\threebars":          "\uE1E8", //  group:System
    "appBarIcons\\fourbars":           "\uE1E9"  //  group:System
}
);

}(this));
