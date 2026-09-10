using SW.PrimitiveTypes;
using System;
using System.Runtime.Serialization;

namespace SW.Bitween
{
    public class BitweenException : SWException
    {
        public BitweenException() {}
        public BitweenException(string message) : base(message) {}
        public BitweenException(string message, Exception innerException) : base(message, innerException) {}
    }

    public class DocumentSizeException : BitweenException {}

    public class AdapterException(int exitCode, string message) : BitweenException($"{exitCode}:{message}")
    {
        public int ExitCode { get; } = exitCode;
    }


    //public class MaximumDocumentSizeExceededException : BitweenException
    //{
    //    public int MessageSize;
    //    public int MaximumMessageSize;
    //    public MaximumDocumentSizeExceededException(int MessageSize, int MaximumMessageSize) : base("Maximum document size exceeded, document size:" + MessageSize + ",documnet size limit: " + MaximumMessageSize)
    //    {
    //        this.MessageSize = MessageSize;
    //        this.MaximumMessageSize = MaximumMessageSize;
    //    }
    //}

    //public class InvalidEntityIdOrPinException : BitweenException
    //{
    //    public InvalidEntityIdOrPinException() : base()
    //    {
    //    }
    //}

    public class DocumentHandlerNotFoundException : BitweenException
    {
        public DocumentHandlerNotFoundException() : base()
        {
        }

    }


    public class UnSupportedDocumentDirectionException : BitweenException
    {
        public int DocumentDirection;
        public UnSupportedDocumentDirectionException(int DocumentDirection)
        {
            this.DocumentDirection = DocumentDirection;
        }



        public UnSupportedDocumentDirectionException() : base()
        {
        }

    }


    public class SubscriberPropertyNotFoundException(int SubscriberID, string PropertyName) : BitweenException
    {
        public int SubscriberID = SubscriberID;
        public string PropertyName = PropertyName;
    }


    //public class DocumentMapException : BitweenException
    //{
    //    public DocumentMapException(string Message) : base(Message)
    //    {
    //    }

    //}


    //public class AccessRequestParseError : BitweenException
    //{
    //    public AccessRequestParseError(string Message) : base(Message)
    //    {
    //    }
    //}


    public class DuplicateDocumentFoundException(int DuplicateId)
        : BitweenException("Duplicate document transmission occurred, interchangelog ID:" + DuplicateId)
    {
    }

    public class PromotedPropertyNotPresent(string Message) : BitweenException(Message)
    {
    }
}
