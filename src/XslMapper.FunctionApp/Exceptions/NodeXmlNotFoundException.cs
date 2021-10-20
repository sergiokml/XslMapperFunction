using System;

namespace XslMapper.FunctionApp.Exceptions
{
    public class NodeXmlNotFoundException : ApplicationException
    {
        public NodeXmlNotFoundException()
            : this("Node Xml not found")
        {
        }

        public NodeXmlNotFoundException(string message)
            : base(message)
        {
        }

    }
}
