import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';

/// Shared PDF layout used by Bills & Statements and Transaction History: a title, consumer
/// identity line, and a simple two-column table -- every value passed in must already be real
/// (from the same backend rows the screen itself shows), this just formats it as a document the
/// user can save/share/print via the OS share sheet (no traditional "Downloads" folder on
/// Android without extra storage permissions, so share is the honest equivalent of "download").
Future<void> shareStatementPdf({
  required String title,
  required String subtitle,
  required List<String> columnHeaders,
  required List<List<String>> rows,
  String? footerNote,
}) async {
  final doc = pw.Document();
  doc.addPage(
    pw.MultiPage(
      pageFormat: PdfPageFormat.a4,
      build: (context) => [
        pw.Text(title, style: pw.TextStyle(fontSize: 20, fontWeight: pw.FontWeight.bold)),
        pw.SizedBox(height: 4),
        pw.Text(subtitle, style: const pw.TextStyle(fontSize: 11, color: PdfColors.grey700)),
        pw.SizedBox(height: 16),
        pw.TableHelper.fromTextArray(
          headers: columnHeaders,
          data: rows,
          headerStyle: pw.TextStyle(fontWeight: pw.FontWeight.bold, fontSize: 10),
          cellStyle: const pw.TextStyle(fontSize: 10),
          headerDecoration: const pw.BoxDecoration(color: PdfColors.grey300),
          cellAlignment: pw.Alignment.centerLeft,
          columnWidths: {for (var i = 0; i < columnHeaders.length; i++) i: const pw.FlexColumnWidth()},
        ),
        if (footerNote != null) ...[
          pw.SizedBox(height: 16),
          pw.Text(footerNote, style: const pw.TextStyle(fontSize: 9, color: PdfColors.grey600)),
        ],
      ],
    ),
  );
  await Printing.sharePdf(bytes: await doc.save(), filename: '${title.replaceAll(' ', '_')}.pdf');
}
