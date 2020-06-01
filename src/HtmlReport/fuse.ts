import { fusebox, sparky } from "fuse-box";

class Context {
  isProduction: boolean;
  runServer: boolean;
  getConfig() {
    return fusebox({
      target: "browser",
      entry: "src/index.ts",
      webIndex: {
        template: "src/index.html",
      },
      cache: {
        root: ".cache",
        enabled: true,
      },
      env: { NODE_ENV: this.isProduction ? "production" : "development" },
      //      watch: true,
      hmr: true,
      devServer: this.runServer,
      logging: { level: "succinct" },
    });
  }
}
const { task, exec } = sparky<Context>(Context);

task("default", async (ctx) => {
  ctx.runServer = true;
  const fuse = ctx.getConfig();
  await fuse.runDev();
});

task("dist", async (ctx) => {
  ctx.runServer = false;
  ctx.isProduction = true;
  const fuse = ctx.getConfig();
  await fuse.runProd({ uglify: true });
});
