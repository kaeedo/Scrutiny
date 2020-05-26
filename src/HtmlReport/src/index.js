"use strict";
exports.__esModule = true;
var d3 = require("d3");
var dagreD3 = require("dagre-d3");
var onlyUnique = function (value, index, self) { return self.indexOf(value) === index; };
window.scrutiny = {
    graph: undefined,
    drawGraph: function (graph, transitions) {
        // Create the input graph
        var g = new dagreD3.graphlib.Graph().setGraph({ rankdir: "LR" });
        var distinctNodes = graph.map(function (g) { return g[0]; }).filter(onlyUnique);
        distinctNodes.forEach(function (dn) { return g.setNode(dn, {}); });
        graph.forEach(function (n) { return g.setEdge(n[0], n[1], {}); });
        // Create the renderer
        var render = new dagreD3.render();
        // Set up an SVG group so that we can translate the final graph.
        var svg = d3.select("svg");
        var inner = svg.append("g");
        // Run the renderer. This is what draws the final graph.
        render(inner, g);
        var xCenterOffset = (parseInt(svg.attr("width"), 10) - g.graph().width) / 2;
        inner.attr("transform", "translate(" + xCenterOffset + ", 20)");
        svg.attr("height", g.graph().height + 40);
        svg.attr("width", g.graph().width + 40);
        window.scrutiny.graph = g;
        document.getElementById("reportRangeSlider").style.width =
            window.scrutiny.graph.graph().width + 40;
    }
};
