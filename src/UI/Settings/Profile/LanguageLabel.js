var _ = require('underscore');
var Handlebars = require('handlebars');
var LanguageCollection = require('./Language/LanguageCollection');

Handlebars.registerHelper('languageLabel', function() {

	var result = '';
	_.each(this.languages, function (wantedLanguage) {
		if (wantedLanguage.allowed)
		{
	var language = LanguageCollection.find(function(lang) {
				return lang.get('name') === wantedLanguage.language.name;
			});
			result += '<li><span class="label label-primary">' + language.get('name') + '</span></li>';
		}
	});

    return new Handlebars.SafeString(result);
});