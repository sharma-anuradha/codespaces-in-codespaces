declare module 'http-message-parser' {
    /*
    httpVersion: 1.1,
    statusCode: 200,
    statusMessage: 'OK',
    method: null,
    url: null,
    headers: {
      'MIME-Version': '1.0'
      'Content-Type': 'multipart/mixed; boundary=frontier'
    },
    body: <Buffer>, // "This is a message with multiple parts in MIME format."
    boundary: 'frontier',
    multipart: [
      {
        headers: {
          'Content-Type': 'text/plain'
        },
        body: <Buffer> // "This is the body of the message."
      },
      {
        headers: {
          'Content-Type': 'application/octet-stream'
          'Content-Transfer-Encoding': 'base64'
        },
        body: <Buffer> // "PGh0bWw+CiAgPGhlYWQ+CiAgPC9oZWFkPgogIDxib2R5Pgog..."
      }
    ]
  }
     */
    export default function(
        str: string
    ): {
        method: string;
        statusCode: number;
        statusMessage: string;
        body: string;
        headers: Record<string, string>;
    };
}
