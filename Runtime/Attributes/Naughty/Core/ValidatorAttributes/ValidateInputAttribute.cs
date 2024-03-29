﻿using System;

namespace NeutronNetwork.Naughty.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ValidateInputAttribute : ValidatorAttribute
    {
        public string CallbackName { get; private set; }
        public string Message { get; private set; }
        public int MessageType { get; private set; }

        public ValidateInputAttribute(string callbackName, string message = null, int messageType = 3)
        {
            CallbackName = callbackName;
            Message = message;
            MessageType = messageType;
        }
    }
}