﻿using NetworkScopes;

[ServerScope(typeof(IMyClientMatch))]
public interface IMyServerMatch
{
    void LeaveMatch();
}