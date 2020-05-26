const { src, task, exec, context } = require("fuse-box/sparky");
const { FuseBox, QuantumPlugin, WebIndexPlugin } = require("fuse-box");

context({
  getConfig() {
    return FuseBox.init({
      homeDir: "src",
      target: "browser@es6",
      sourceMaps: true,
      output: "dist/$name.js",
      plugins: [
        WebIndexPlugin({
          template: "src/index.html"
        }),
        this.isProduction &&
          QuantumPlugin({
            uglify: true,
            bakeApiIntoBundle: "app"
          })
      ]
    });
  }
});

task("default", async context => {
  const fuse = context.getConfig();
  fuse
    .bundle("app")
    .hmr()
    .watch()
    .instructions("> index.ts");
  fuse.dev();
  await fuse.run();
});

task("dist", async context => {
  await src("./dist")
    .clean("dist/")
    .exec();

  context.isProduction = true;
  const fuse = context.getConfig();
  fuse.bundle("app").instructions("> index.ts");

  await fuse.run();
});
