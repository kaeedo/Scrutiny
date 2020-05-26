import * as d3 from "d3";
import * as dagreD3 from "dagre-d3";

interface Transition {
  from: string;
  to: string;
  error?: string;
}

declare global {
  interface Window {
    scrutiny: {
      init(graph: string[][], transitions: Transition[]): void;
      graph: any;
      transitions: Transition[];
    };
  }
}

const onlyUnique = (value, index, self) => self.indexOf(value) === index;

const resetColors = (transitions) => {
  transitions.forEach((t) => {
    const fromNode = window.scrutiny.graph.node(t.from);
    const edge = window.scrutiny.graph.edge(t.from, t.to);
    const toNode = window.scrutiny.graph.node(t.to);

    fromNode.elem.firstChild.style.fill = "none";
    edge.elem.firstChild.style.stroke = "black";
    edge.elem.lastChild.firstChild.style.fill = "black";
    toNode.elem.firstChild.style.fill = "none";
  });
};

const setColors = (transitions) => {
  transitions.forEach((t) => {
    const fromNode = window.scrutiny.graph.node(t.from);
    const edge = window.scrutiny.graph.edge(t.from, t.to);
    const toNode = window.scrutiny.graph.node(t.to);

    if (t.error) {
      edge.elem.firstChild.style.stroke = "red";
      edge.elem.lastChild.firstChild.style.fill = "red";
      toNode.elem.firstChild.style.fill = "red";
    } else {
      fromNode.elem.firstChild.style.fill = "lightblue";
      toNode.elem.firstChild.style.fill = "lightblue";
    }
  });
};

const onChangeSlider = (e) => {
  const stepsDiv = document.getElementById("steps");
  stepsDiv.innerHTML = "<span>Steps:&nbsp;</span>";
  const value = parseInt(e.target.value, 10);
  const subTransitions = window.scrutiny.transitions.slice(0, value);

  subTransitions.forEach((v) => {
    const span = document.createElement("span");
    span.innerHTML = v.from + " --> " + v.to + ",&nbsp;";
    stepsDiv.appendChild(span);
  });

  resetColors(window.scrutiny.transitions);
  setColors(subTransitions);
};

window.scrutiny = {
  graph: undefined,
  transitions: [],
  init: (graph, transitions) => {
    // Create the input graph
    const g = new dagreD3.graphlib.Graph().setGraph({ rankdir: "LR" });

    const distinctNodes = graph.map((g) => g[0]).filter(onlyUnique);

    distinctNodes.forEach((dn) => g.setNode(dn, {}));

    graph.forEach((n) => g.setEdge(n[0], n[1], {}));

    // Create the renderer
    const render = new dagreD3.render();

    // Set up an SVG group so that we can translate the final graph.
    const svg = d3.select("svg");
    const inner = svg.append("g");

    // Run the renderer. This is what draws the final graph.
    render(inner, g);
    const xCenterOffset =
      (parseInt(svg.attr("width"), 10) - g.graph().width) / 2;
    inner.attr("transform", "translate(" + xCenterOffset + ", 20)");
    svg.attr("height", g.graph().height + 40);
    svg.attr("width", g.graph().width + 40);

    window.scrutiny.graph = g;

    const slider = document.getElementById("reportRangeSlider");
    slider.setAttribute("max", transitions.length.toString());
    slider.style.width = `${window.scrutiny.graph.graph().width + 40}px`;

    slider.oninput = onChangeSlider;
    window.scrutiny.transitions = transitions;
  },
};
