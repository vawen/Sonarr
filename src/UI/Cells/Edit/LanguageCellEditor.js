var _ = require('underscore');
var Backbone = require('backbone');
var Backgrid = require('backgrid');
var Marionette = require('marionette');
var ProfileSchemaCollection = require('../../Settings/Profile/ProfileSchemaCollection');

module.exports = Backgrid.CellEditor.extend({
    className : 'language-cell-editor',
    template  : 'Cells/Edit/LanguageCellEditorTemplate',
    tagName   : 'select',

    events : {
        'change'  : 'save',
        'blur'    : 'close',
        'keydown' : 'close'
    },

    render : function() {
        var self = this;

        var profileSchemaCollection = new ProfileSchemaCollection();
        var promise = profileSchemaCollection.fetch();

        promise.done(function() {
            var templateName = self.template;
            self.schema = profileSchemaCollection.first();

			var selected = _.find(self.schema.get('languages'), function(model) {
				return model.id === self.model.get(self.column.get('name')).id;
			});

			if (selected) {
				selected.selected = true;
			}

			self.templateFunction = Marionette.TemplateCache.get(templateName);
			var data = self.schema.toJSON();
			var html = self.templateFunction(data);
			self.$el.html(html);
		});
        
		return this;
    },

    save : function(e) {
        var model = this.model;
        var column = this.column;
        var selected = parseInt(this.$el.val(), 10);

        var profileItem = _.find(this.schema.get('languages'), function(model) {
            return model.id === selected;
        });

		profileItem.revision = {
                version : 1,
                real    : 0
            };

		var colName = column.get('name');
		
        model.set(column.get('name'), profileItem);
        model.save();

        model.trigger('backgrid:edited', model, column, new Backgrid.Command(e));
    },

    close : function(e) {
        var model = this.model;
        var column = this.column;
        var command = new Backgrid.Command(e);

        model.trigger('backgrid:edited', model, column, command);
    }
});