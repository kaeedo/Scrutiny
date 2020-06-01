import * as d3 from "d3";
import * as dagreD3 from "dagre-d3";

interface PageState {
  Name: string;
}

interface Transition {
  From: PageState;
  To: PageState;
  Error?: Error;
}

interface Error {
  Case: string;
  Fields: [string, string];
}

interface Report {
  Graph: [PageState, PageState[]][];
  PerformedTransitions: Transition[];
}

declare global {
  interface Window {
    scrutiny: {
      init(report: Report): void;
      graph: any;
      transitions: Transition[];
    };
  }
}

const onlyUnique = (value, index, self) => self.indexOf(value) === index;

const resetColors = (transitions: Transition[]) => {
  transitions.forEach((t) => {
    const fromNode = window.scrutiny.graph.node(t.From.Name);
    const edge = window.scrutiny.graph.edge(t.From.Name, t.To.Name);
    const toNode = window.scrutiny.graph.node(t.To.Name);

    fromNode.elem.firstChild.style.fill = "none";
    edge.elem.firstChild.style.stroke = "black";
    edge.elem.firstChild.style['stroke-width'] = '2px';
    edge.elem.lastChild.firstChild.style.fill = "black";
    toNode.elem.firstChild.style.fill = "none";
  });
};

const setColors = (transitions: Transition[]) => {
  transitions.forEach((t) => {
    switch (t.Error?.Case) {
      case "State":
        const errorNode = window.scrutiny.graph.node(t.Error.Fields[0]);
        errorNode.elem.firstChild.style.fill = "red";
        break;
      case "Transition":
        const fromNode = window.scrutiny.graph.node(t.Error.Fields[0]);
        const edge = window.scrutiny.graph.edge(
          t.Error.Fields[0],
          t.Error.Fields[1]
        );
        const toNode = window.scrutiny.graph.node(t.Error.Fields[1]);
        edge.elem.firstChild.style.stroke = "red";
        edge.elem.firstChild.style['stroke-width'] = "4px";
        edge.elem.lastChild.firstChild.style.fill = "red";
        //fromNode.elem.firstChild.style.fill = "red";
        //toNode.elem.firstChild.style.fill = "red";
        break;
      default:
        const from = window.scrutiny.graph.node(t.From.Name);
        const to = window.scrutiny.graph.node(t.To.Name);
        from.elem.firstChild.style.fill = "lightblue";
        to.elem.firstChild.style.fill = "green";
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
    span.innerHTML = `${v.From.Name} --> ${v.To.Name},&nbsp;`;
    stepsDiv.appendChild(span);
  });

  resetColors(window.scrutiny.transitions);
  setColors(subTransitions);
};

const getGraph = (graph: [PageState, PageState[]][]): string[][] => {
  const final = [];

  graph.forEach((g) => {
    const ps = g[0].Name;
    g[1].forEach((d) => final.push([ps, d.Name]));
  });

  return final;
};

window.scrutiny = {
  graph: undefined,
  transitions: [],
  init: (report: Report) => {
    const graph = getGraph(report.Graph);

    const transitions = report.PerformedTransitions;
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
