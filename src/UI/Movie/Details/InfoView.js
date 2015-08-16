var Marionette = require('marionette');

module.exports = Marionette.ItemView.extend({
    template : 'Movie/Details/InfoViewTemplate',

    initialize : function(options) {
        this.movieFileCollection = options.movieFileCollection;
        this.quality = null;
        this.sizeOnDisk = 0;

        this.listenTo(this.model, 'change', this.render);
        this.listenTo(this.movieFileCollection, 'sync', this.syncDone);
    },

    setQuality : function () {
        var movieFile = this.movieFileCollection.get(this.model.get('movieFileId'));
        if (movieFile) {
            this.quality = movieFile.get('quality');
            this.sizeOnDisk = movieFile.get('size');
        }
    },

    syncDone : function () {
        this.setQuality();
        this.render();
    },

    templateHelpers : function() {
        return {
            fileCount : this.movieFileCollection.length,
            quality : this.quality,
            sizeOnDisk : this.sizeOnDisk
        };
    }
});