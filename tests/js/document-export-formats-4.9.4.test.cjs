'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const web = relative => fs.readFileSync(path.join(root, 'src/PublisherStudio.Web', relative), 'utf8');

const documentKinds = ['text', 'pdf', 'rtf', 'odt', 'html', 'txt', 'epub', 'doc', 'mht'];
const spreadsheetKinds = ['spreadsheet', 'xls', 'csv'];

test('4.9.4 accepts every office kind and keeps XLSX as the native spreadsheet object', () => {
    const insertion = web('Services/OrganicPlugins/PublisherAiDocumentInsertionService.cs');
    for (const kind of [...documentKinds, ...spreadsheetKinds])
        assert.match(insertion, new RegExp(`"${kind}"`), `kind ${kind} is accepted`);
    assert.match(insertion, /var isSpreadsheet = kind is "spreadsheet" or "xls" or "csv";/);
    // Every spreadsheet kind still inserts the native XLSX object into the publication.
    assert.match(insertion, /if \(isSpreadsheet\)\s*\{\s*var fileName[\s\S]*?editor\.AddSpreadsheet\(bytes, [\s\S]*?"\.xlsx", SpreadsheetStorageFormat\.Xlsx\)/);
    assert.match(insertion, /"xls" => spreadsheets\.ConvertXlsxToXls\(bytes\)/);
    assert.match(insertion, /"csv" => new System\.Text\.UTF8Encoding\(encoderShouldEmitUTF8Identifier: false\)\.GetBytes\(normalizedCsv\)/);
    assert.match(insertion, /_ => richText\.Export\(storyContent, kind\)/);
});

test('4.9.4 renders every rich-text kind through the server-side RichEdit engine', () => {
    const factory = web('Services/RichTextDocumentFactory.cs');
    const exporter = factory.slice(factory.indexOf('public byte[] Export(byte[] openXml, string kind)'), factory.indexOf('Creates open XML using'));
    assert.match(exporter, /if \(kind == "text"\)\s*return openXml;/);
    assert.match(exporter, /if \(kind == "pdf"\)\s*return ExportPdf\(openXml\);/);
    for (const [kind, format] of [['rtf', 'Rtf'], ['odt', 'OpenDocument'], ['html', 'Html'], ['epub', 'ePub'], ['doc', 'Doc'], ['mht', 'Mht']])
        assert.match(exporter, new RegExp(`"${kind}" => DevExpress\\.XtraRichEdit\\.DocumentFormat\\.${format},`));
    assert.match(exporter, /EmbedImages = true;/, 'downloaded HTML is self-contained');
});

test('4.9.4 spreadsheet import and export formats are unchanged', () => {
    const models = web('BusinessObjects/PublicationModels.cs');
    assert.match(models, /public enum SpreadsheetStorageFormat \{ Xlsx, Xlsm, Xls, Csv, Text \}/);
    const service = web('Services/SpreadsheetDocumentService.cs');
    for (const member of ['public byte[] CreateXlsxFromCsv(string csv)', 'public void ValidateWorkbookContent(byte[]? content, SpreadsheetStorageFormat format)', 'public byte[] ConvertXlsxToXls(byte[] xlsx)'])
        assert.ok(service.includes(member), member);
    assert.match(service, /workbook\.SaveDocument\(output, DevExpress\.Spreadsheet\.DocumentFormat\.Xls\);/);
});

test('4.9.4 the 1-Wire function catalog advertises the same kinds the service accepts', () => {
    const catalog = JSON.parse(web('Configuration/publisher-dx-functions.json').replace(/^﻿/, ''));
    const items = Array.isArray(catalog) ? catalog : Object.values(catalog).find(Array.isArray);
    const create = items.find(item => item.key === 'publisher.document.create');
    assert.ok(create, 'publisher.document.create is in the catalog');
    const advertised = JSON.parse(create.parameterSchemaJson).properties.kind.enum;
    assert.deepEqual([...advertised].sort(), [...documentKinds, ...spreadsheetKinds].sort());
});
