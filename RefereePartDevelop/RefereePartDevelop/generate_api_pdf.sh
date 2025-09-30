#!/bin/bash

# Navigate to your project directory
cd /home/RefereePart/RefereePart

# Create output directory for PDFs
mkdir -p pdf_output

# Check if wkhtmltopdf is installed, if not install it
if ! command -v wkhtmltopdf &> /dev/null; then
    echo "Installing wkhtmltopdf..."
    sudo apt-get update
    sudo apt-get install -y wkhtmltopdf
fi

# Convert each HTML file in the _site/api directory to a PDF
echo "Converting HTML files to PDFs..."
for html_file in _site/api/*.html; do
    if [ -f "$html_file" ]; then
        base_name=$(basename "$html_file" .html)
        echo "Processing $base_name"
        # Use absolute path for reliable file access
        absolute_path=$(realpath "$html_file")
        wkhtmltopdf --enable-local-file-access "file://$absolute_path" "pdf_output/${base_name}.pdf"
    fi
done

# Check if we have PDFs to merge
pdf_count=$(ls -1 pdf_output/*.pdf 2>/dev/null | wc -l)
if [ $pdf_count -gt 0 ]; then
    # Install pdftk if not already installed
    if ! command -v pdftk &> /dev/null; then
        echo "Installing pdftk..."
        sudo apt-get install -y pdftk
    fi

    # Sort the PDFs to ensure a consistent order (optional)
    # First, create a TOC page if needed
    if [ -f "_site/api/toc.html" ]; then
        echo "Creating TOC PDF..."
        wkhtmltopdf --enable-local-file-access "file://$(realpath _site/api/toc.html)" "pdf_output/000_TableOfContents.pdf"
    fi

    # Merge all PDFs into a single document
    echo "Merging all PDFs into one document..."
    pdftk pdf_output/*.pdf cat output RefereePart_API_Documentation.pdf
    echo "Conversion complete! Final PDF is at: $(pwd)/RefereePart_API_Documentation.pdf"
else
    echo "No PDF files were generated. Check if the HTML files exist in _site/api/"
fi
