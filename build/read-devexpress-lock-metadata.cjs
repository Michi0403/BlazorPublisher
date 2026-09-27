"use strict";
const fs = require("fs");

function fail(message) {
  process.stderr.write(String(message) + "\n");
  process.exit(2);
}

const lockPath = process.argv[2];
if (!lockPath) fail("package-lock.json path argument is required.");

let lock;
try {
  lock = JSON.parse(fs.readFileSync(lockPath, "utf8"));
} catch (error) {
  fail(`Could not parse ${lockPath}: ${error && error.message ? error.message : error}`);
}

function readPackage(name) {
  const packages = lock && lock.packages && typeof lock.packages === "object" ? lock.packages : {};
  const entry = packages[`node_modules/${name}`] || {};
  return {
    version: String(entry.version || ""),
    integrity: String(entry.integrity || ""),
    resolved: String(entry.resolved || "")
  };
}

process.stdout.write(JSON.stringify({
  devExtreme: readPackage("devextreme-dist"),
  spreadsheet: readPackage("devexpress-aspnetcore-spreadsheet")
}));
