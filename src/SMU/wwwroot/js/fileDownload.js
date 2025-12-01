/**
 * File Download Helper
 * Provides JavaScript interop for downloading files from byte arrays
 */

window.fileDownload = {
    /**
     * Download a file from a byte array
     * @param {string} fileName - Name of the file to download
     * @param {string} contentType - MIME type of the file
     * @param {string} base64String - Base64 encoded file content
     */
    downloadFromByteArray: function (fileName, contentType, base64String) {
        try {
            // Convert base64 to byte array
            const byteCharacters = atob(base64String);
            const byteNumbers = new Array(byteCharacters.length);
            
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            
            const byteArray = new Uint8Array(byteNumbers);
            
            // Create blob
            const blob = new Blob([byteArray], { type: contentType });
            
            // Create download link
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            
            // Trigger download
            document.body.appendChild(link);
            link.click();
            
            // Cleanup
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
            
            return true;
        } catch (error) {
            console.error('Download failed:', error);
            return false;
        }
    }
};
