const esbuild = require('esbuild');
const polyfillNode = require('esbuild-plugin-polyfill-node').polyfillNode;
const path = require('path');

esbuild.build({
  entryPoints: [path.join(__dirname, '../src/wrapper.js')],
  bundle: true,
  outfile: path.join(__dirname, '../build.js'),
  format: 'esm',
  platform: 'browser', // specify browser platform
  plugins: [
    polyfillNode({
      polyfills: {
        crypto: true,
        process: true,
        fs: true,
        buffer: true,
      },
    }),
  ],
}).catch(() => process.exit(1));