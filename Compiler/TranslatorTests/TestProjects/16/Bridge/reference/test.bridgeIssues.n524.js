﻿(function (globals) {
    "use strict";

    Bridge.define('Test.BridgeIssues.N524.Bridge524', {
        statics: {
            callAsGetter: function () {
                var list = new (Test.BridgeIssues.N524.Bridge524.ImmutableList$1(System.Int32))();
                var firstValue = list[0];
            }
        }
    });
    
    
    
    Bridge.init();
})(this);
