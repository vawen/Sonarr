var BackboneSortableCollectionView = require('backbone.collectionview');
var EditProfileLanguageView = require('./EditProfileLanguageView');

module.exports = BackboneSortableCollectionView.extend({
    className : 'qualities',
    modelView : EditProfileLanguageView,

    attributes : {
        'validation-name' : 'languages'
    },

    events : {
        'click li, td'    : '_listItem_onMousedown',
        'dblclick li, td' : '_listItem_onDoubleClick',
        'keydown'         : '_onKeydown'
    }
});