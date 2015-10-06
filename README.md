# JsAndCssCombiner
A javascript and css combiner and minifier that requires no modification to your html nor configuration of any bundles. Has some additional features such as cdn host prepending to all images and output caching to the combined js and css.

In order to apply compression and bundling to a page you need to:

<b>In MVC:</b>
Use an attribute [Combine(...)] on any action method 
and passing true/false for the desired options.

There are three constructors that can be used for the mvc attribute:

Combiner() => will turn all features to on ( 'PrependCdnHostToImages' in the web.config must be set to some url

Combiner(bool applyOutputCaching) => will turn all features on and you can control the 'applyOutputCaching' feature

Combiner(bool applyOutputCaching, bool combineJs, bool combineCss, bool versionOnly, bool minifyJs, bool minifyCss, bool prependCdnHostToImages)

Note: the 'prependCdnHostToImages' feature will be ignored if the 'imagesCdnHostToPrepend' web.config value is blank.

<b>In Webforms:</b> 
Use the server control CombinerWebControl and set the desired options.

Check out the sample MVC app and it's configuration file to see how to cnfigure this tool.

<b>This tool uses</b>
- <b>EntLib</b> for logging errors (errors by default will be logged to errors.txt file, can be changed in logging.config), 
- <b>HtmlAgilityPack</b> for parsing the Dom and 
- <b>Yui.Compressor</b> for compressing the js and css.

There is a combinerSettings.txt file that can be used to turn on/off the various features 'live'
without having to mess with the web.config.

This file can also be used to specify a 'CssVersion' or 'JsVersion' which will fingerprint the combined url 
(append a query to the resource url).
This version setting will also fingerprint the urls or the individual resources even when the tools's 
other settings are turned off, as long as the 'VersionOnly' setting is set to true. This 'VersionOnly' setting
is only applicable when the 'CombineJs' and 'CombineCss' are set to false, and is ignored otherwise (all resources will be versioned).

In the web.config's combinerSettings node there is an attribute 'imagesCdnHostToPrepend' 
which can be used to specify a subdomain or cdn url that will be used to replace the
image's root paths (this feature must be set to on in the attribute filter or web control).
See the 'prependCdnHostToImages' attribute filter parameter or the 'PrependCdnHostToImages'
property of the web control.

<b>Troubleshooting tips:</b>

- Do not use @import directives inside your css files since they will not work when the css is combined, since
they must be at the top of any css in order to work. Or make sure they appear at the top of the first css on the page.

- Do not use ie specific css directives such as 'filter' since non-IE browsers stop parsing the css file on which
they are declared. Move them to IE specific files and add them to the page inside IE conditional comments such as:
<!--[if IE]><link type='text/css' rel='stylesheet' href='/someFile.css'/><![endif]-->
