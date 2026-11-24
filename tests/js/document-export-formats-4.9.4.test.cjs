'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const web = relative => fs.readFileSync(path.join(root, 'src/PublisherStudio.Web', relative), 'utf8');

const documentKinds = ['text', 'rtf', 'html', 'txt'];
const spreadsheetKinds = ['spreadsheet', 'csv'];

test('4.9.4 accepts supported document kinds and keeps XLSX as the native spreadsheet object', () => {
    const insertion = web('Services/OrganicPlugins/PublisherAiDocumentInsertionService.cs');
    for (const kind of [...documentKinds, ...spreadsheetKinds])
        assert.match(insertion, new RegExp(`"${kind}"`), `kind ${kind} is accepted`);
    assert.match(insertion, /var isSpreadsheet = kind is "spreadsheet" or "csv";/);
    // Every spreadsheet kind still inserts the native XLSX object into the publication.
    assert.match(insertion, /if \(isSpreadsheet\)\s*\{\s*var fileName[\s\S]*?editor\.AddSpreadsheet\(bytes, [\s\S]*?"\.xlsx", SpreadsheetStorageFormat\.Xlsx\)/);
    assert.match(insertion, /"csv" => new System\.Text\.UTF8Encoding\(encoderShouldEmitUTF8Identifier: false\)\.GetBytes\(normalizedCsv\)/);
    assert.match(insertion, /_ => await exportStoryAsync\(storyContent, kind\)/);
});

test('4.9.4 exports RTF and HTML through the embedded RichEdit', () => {
    const factory = web('Services/RichTextDocumentFactory.cs');
    const bridge = web('Components/OrganicPlugins/OrganicDocumentCreationBridge.razor');
    assert.doesNotMatch(factory, /RichEditDocumentServer/);
    assert.match(bridge, /<DxRichEdit @ref="_exportEditor"/);
    assert.match(bridge, /LoadDocumentAsync\(content, DocumentFormat.OpenXml, timeout.Token\)/);
    assert.match(bridge, /kind == "rtf" \? DocumentFormat.Rtf : DocumentFormat.Html/);
    assert.match(bridge, /ExportDocumentAsync\(format, timeout.Token\)/);
});

test('4.9.4 spreadsheet import and export formats are unchanged', () => {
    const models = web('BusinessObjects/PublicationModels.cs');
    assert.match(models, /public enum SpreadsheetStorageFormat \{ Xlsx, Xlsm, Xls, Csv, Text \}/);
    const service = web('Services/SpreadsheetDocumentService.cs');
    for (const member of ['public byte[] CreateXlsxFromCsv(string csv)', 'public void ValidateWorkbookContent(byte[]? content, SpreadsheetStorageFormat format)'])
        assert.ok(service.includes(member), member);
});

test('4.9.4 the 1-Wire function catalog advertises the same kinds the service accepts', () => {
    const catalog = JSON.parse(web('Configuration/publisher-dx-functions.json').replace(/^﻿/, ''));
    const items = Array.isArray(catalog) ? catalog : Object.values(catalog).find(Array.isArray);
    const create = items.find(item => item.key === 'publisher.document.create');
    assert.ok(create, 'publisher.document.create is in the catalog');
    const advertised = JSON.parse(create.parameterSchemaJson).properties.kind.enum;
    assert.deepEqual([...advertised].sort(), [...documentKinds, ...spreadsheetKinds].sort());
});

test('1-Wire file generation avoids the standalone DevExpress Office File API', () => {
    const project = web('PublisherStudio.Web.csproj');
    assert.doesNotMatch(project, /<PackageReference\s+Include="DevExpress\.Document\.Processor(?:\.|")/i);

    for (const file of [
        'Services/OrganicPlugins/PublisherAiDocumentInsertionService.cs',
        'Services/RichTextDocumentFactory.cs',
        'Services/SpreadsheetDocumentService.cs',
        'Components/OrganicPlugins/OrganicDocumentCreationBridge.razor'
    ]) {
        const source = web(file);
        assert.doesNotMatch(source, /\b(?:RichEditDocumentServer|PdfDocumentProcessor)\b/, file);
        assert.doesNotMatch(source, /\bnew\s+(?:DevExpress\.Spreadsheet\.)?Workbook\s*\(/, file);
    }
});
