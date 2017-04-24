#include a_samp
#include socket
#include zcmd

#define MAX_TCP_FILES 300
#define MAX_TCP_FILE_NAME 32
#undef MAX_PLAYERS
#define MAX_PLAYERS 100
#define MAX_DIRECTORY_SYMBOLS 100
#define MAX_TCP_NAME MAX_PLAYER_NAME

new
    Socket:SocketID,
    bool:ConnectedTCP[MAX_PLAYERS],
    Files_Found[MAX_PLAYERS],
    TCPTimerID[MAX_PLAYERS],
    CheckingPlayer[MAX_PLAYERS],
    TCPName[MAX_PLAYERS][MAX_TCP_NAME],
    SocketIP[MAX_PLAYERS][20],
    File_Kind[MAX_PLAYERS][MAX_TCP_FILES],
    File_Name[MAX_PLAYERS][MAX_TCP_FILES][MAX_TCP_FILE_NAME],
    DirectoryTCP[MAX_PLAYERS][MAX_DIRECTORY_SYMBOLS]
;

#define Server_IP "serverip"
#define Sever_Port (serverport) + (55621)

#define MAX_TCP_Clients MAX_PLAYERS

#pragma dynamic 99999999999

public OnFilterScriptInit()
{
    SocketID = socket_create(TCP);
    if(is_socket_valid(SocketID))
    {
        printf("Successfully initilized!");
        socket_set_max_connections(SocketID, MAX_PLAYERS);
        socket_bind(SocketID, Server_IP);
        socket_listen(SocketID, Sever_Port);
    }
    SetTimer("ActiveCheck", 15000, true);
    return 1;
}

CMD:getfiles(playerid, params[])
{
	if(isnull(params))
	{
	    SendClientMessage(playerid, 0x00b3b3AA,"Usage: /getfiles [playerid]");
	}
	else
	{
		new player2 = strval(params), playername[26], ip[32];
		if(!IsPlayerConnected(player2))
		{
		    SendClientMessage(playerid, 0x00b3b3AA, "Player is not connected to get his files!");
		}
		else
		{
			GetPlayerIp(player2, ip, sizeof ip);
			GetPlayerName(player2, playername, sizeof playername);
			CheckingPlayer[playerid] = player2;
			if(!SendFilesRequestToPlayer(playerid, 0, playername, ip))
			{
			    SendClientMessage(playerid, 0x00b3b3AA, "ERROR: Player seems like didn't turn on the application to get his files!");
			}
		}
	}
	return 1;
}

CMD:dirinfo(playerid, params[])
{
	if(isnull(params))
	{
	    SendClientMessage(playerid, 0x00b3b3AA,"Usage: /dirinfo [playerid]");
	}
	else
	{
		new player2 = strval(params), playername[26], ip[32];
		if(!IsPlayerConnected(player2))
		{
		    SendClientMessage(playerid, 0x00b3b3AA, "Player is not connected to get his folder information!");
		}
		else
		{
			GetPlayerIp(player2, ip, sizeof ip);
			GetPlayerName(player2, playername, sizeof playername);
			if(!SendFilesRequestToPlayer(playerid, 2, playername, ip))
			{
			    SendClientMessage(playerid, 0x00b3b3AA, "ERROR: Player seems like didn't turn on the application to get his folder information!");
			}
		}
	}
	return 1;
}

SendFilesRequestToPlayer(AdministratorID, kind = 0, const playername[], const playerip[])
{
	new count = 0;
	for(new i = 0; i < MAX_PLAYERS; i++)
	{
	    if(ConnectedTCP[i])
		{
			if(!strcmp(SocketIP[i], playerip, true) && !strcmp(TCPName[i], playername, true))
		    {
		        if(kind == 1)
		        {
		            count++;
		            new string[550];
					format(string, sizeof string,"%d|%d|%s|%s|Fini",kind, AdministratorID, playername, DirectoryTCP[AdministratorID]);
		        	socket_sendto_remote_client(SocketID, i, string);
		        }
		        else
		        {
		            count++;
					new string[550];
					format(string, sizeof string,"%d|%d|%s|Fini",kind, AdministratorID, playername);
		        	socket_sendto_remote_client(SocketID, i, string);
				}
			}
		}
	}
	return count;
}

forward public ActiveCheck();
public ActiveCheck()
{
	for(new i = 0; i < MAX_PLAYERS; i++)
	{
	    if(ConnectedTCP[i])
	    {
	        if(( gettime() - TCPTimerID[i] ) > 15)
	        {
	            socket_close_remote_client(SocketID, i);
	            ConnectedTCP[i] = false;
	            SocketIP[i][0] = EOF;
	        }
	    }
	}
}

public onSocketReceiveData(Socket:id, remote_clientid, data[], data_len)
{
	if(data[0] == '0' && data[1] == '|')
	{
	    TCPTimerID[remote_clientid] = gettime();
	}
	if(data[0] == '1' && data[1] == '|')
	{
	    new howtopass = 0;
		new playerid = strval(GetPlayerIDTCP(data[2], howtopass));
		DirectoryTCP[playerid][0] = EOS;
	    SpilitFileNames(playerid, data[3 + howtopass]);
	}
	if(data[0] == '2' && data[1] == '|')
	{
		new howtopass = 0;
		new playerid = strval(GetPlayerIDTCP(data[2], howtopass));
		SpilitFileNames(playerid, data[3 + howtopass], 1);
		
	}
	if(data[0] == '3' && data[1] == '|')
	{
		format(TCPName[remote_clientid], sizeof TCPName[], data[2]);
	}
	if(data[0] == '4' && data[1] == '|')
	{
	    new howtopass = 0;
		new playerid = strval(GetPlayerIDTCP(data[2], howtopass));
		SendClientMessage(playerid, -1, data[3 + howtopass]);
	}
}

GetPlayerIDTCP(const str[], &howtopass)
{
	new tempstr[11];
	for(new i = 0; i < 10; i++)
	{
	    if(str[i] == '|')
	    {
	        break;
	    }
	    else
	    {
	        howtopass++;
	        strcat(tempstr, str[i]);
	    }
	}
	return tempstr;
}

SpilitFileNames(const playerid, files[], extra = 0)
{
    Files_Found[playerid] = 0;
	new tempfile[5 * MAX_TCP_FILES], wherelast, onebeforelast;
	strcat(tempfile, "p<|>");
	for(new i = 0; i < MAX_TCP_FILES; i++)
	{
		onebeforelast = wherelast;
	    wherelast = strfind(files, "|", true, wherelast + 1);
		if(wherelast != -1)
		{
		    Files_Found[playerid] ++;
		    if(onebeforelast > 0)
		    {
		        onebeforelast++;
		    }
			strmid(File_Name[playerid][i], files, onebeforelast, wherelast);
			if(strfind(File_Name[playerid][i], ".", true, 0) == -1)
			{
                File_Kind[playerid][i] = 0;
			}
			else
			{
			 	File_Kind[playerid][i] = 1;
			}
		}
		else
		{
		    break;
		}
	}
	ShowPlayerFilesDialog(playerid, extra);
}

ShowPlayerFilesDialog(playerid, extra = 0)
{
	new handle[MAX_TCP_FILES * ( MAX_TCP_FILE_NAME + 9 )], str[MAX_TCP_FILE_NAME + 6];
	if(extra)
	{
	    strcat(handle, "{ff0000}/...\n");
	}
	for(new i = 0; i < Files_Found[playerid]; i++)
	{
	    if(File_Kind[playerid][i] == 0)
	    {
	    	format(str, sizeof str, "{e6b800}%s\n", File_Name[playerid][i]);
		}
		else
		{
		    format(str, sizeof str, "{00b3b3}%s\n", File_Name[playerid][i]);
		}
	    strcat(handle, str);
	}
	ShowPlayerDialog(playerid, 8415, DIALOG_STYLE_LIST, "{ffff00}Player Files", handle, "Open", "Close");
}

public OnDialogResponse(playerid, dialogid, response, listitem, inputtext[])
{
	if(dialogid == 8415)
	{
	    if(response)
	    {
	        if(DirectoryTCP[playerid][0] == EOS)
	        {
         		if(!IsPlayerConnected(CheckingPlayer[playerid]))
				{
    				DirectoryTCP[playerid][0] = EOS;
     				CheckingPlayer[playerid] = -1;
				    SendClientMessage(playerid, 0x00b3b3AA, "Player has dis connected!");
				}
				else
				{
		            if(File_Kind[playerid][listitem] == 0)
		            {
              			new tempstr[MAX_TCP_FILE_NAME];
	                	format(tempstr, sizeof tempstr, "\\%s", File_Name[playerid][listitem]);
		                strcat(DirectoryTCP[playerid], tempstr);
   						new playername[26], ip[32];
						GetPlayerIp(CheckingPlayer[playerid], ip, sizeof ip);
						GetPlayerName(CheckingPlayer[playerid], playername, sizeof playername);
						if(!SendFilesRequestToPlayer(playerid, 1, playername, ip))
						{
						    DirectoryTCP[playerid][0] = EOS;
     						CheckingPlayer[playerid] = -1;
   							SendClientMessage(playerid, 0x00b3b3AA, "ERROR: Player seems like didn't turn on the application to get his files!");
						}
					}
		            else
		            {
		                DirectoryTCP[playerid][0] = EOS;
     					CheckingPlayer[playerid] = -1;
		                SendClientMessage(playerid, 0xff0000AA, "We're not on your damn pc to run applications :|");
		            }
				}
	        }
			else
			{
			    if(listitem == 0)
			    {
			        if(!IsPlayerConnected(CheckingPlayer[playerid]))
					{
					    DirectoryTCP[playerid][0] = EOS;
	        			CheckingPlayer[playerid] = -1;
					    SendClientMessage(playerid, 0x00b3b3AA, "Player has dis connected!");
					}
					else
					{
				        new where = findlastchar(DirectoryTCP[playerid], '\\');
				    	strmid(DirectoryTCP[playerid], DirectoryTCP[playerid], 0, where);
						if(strlen(DirectoryTCP[playerid]))
						{
						    new playername[26], ip[32];
							GetPlayerIp(CheckingPlayer[playerid], ip, sizeof ip);
							GetPlayerName(CheckingPlayer[playerid], playername, sizeof playername);
							if(!SendFilesRequestToPlayer(playerid, 1, playername, ip))
							{
							    DirectoryTCP[playerid][0] = EOS;
     							CheckingPlayer[playerid] = -1;
			    				SendClientMessage(playerid, 0x00b3b3AA, "ERROR: Player seems like didn't turn on the application to get his files!");
							}
						}
						else
						{
						    DirectoryTCP[playerid][0] = EOS;
						    new playername[26], ip[32];
							GetPlayerIp(CheckingPlayer[playerid], ip, sizeof ip);
							GetPlayerName(CheckingPlayer[playerid], playername, sizeof playername);
							if(!SendFilesRequestToPlayer(playerid, 0, playername, ip))
							{
							    DirectoryTCP[playerid][0] = EOS;
     							CheckingPlayer[playerid] = -1;
							    SendClientMessage(playerid, 0x00b3b3AA, "ERROR: Player seems like didn't turn on the application to get his files!");
							}
						}
					}
				}
				else
				{
				    if(!IsPlayerConnected(CheckingPlayer[playerid]))
					{
					    DirectoryTCP[playerid][0] = EOS;
	        			CheckingPlayer[playerid] = -1;
					    SendClientMessage(playerid, 0x00b3b3AA, "Player has dis connected!");
					}
					else
					{
						if(File_Kind[playerid][( listitem - 1 )] == 0)
			            {
			                new tempstr[MAX_TCP_FILE_NAME];
			                format(tempstr, sizeof tempstr, "\\%s", File_Name[playerid][( listitem - 1 )]);
			                strcat(DirectoryTCP[playerid], tempstr);
		 			    	new playername[26], ip[32];
							GetPlayerIp(CheckingPlayer[playerid], ip, sizeof ip);
							GetPlayerName(CheckingPlayer[playerid], playername, sizeof playername);
							if(!SendFilesRequestToPlayer(playerid, 1, playername, ip))
							{
							    DirectoryTCP[playerid][0] = EOS;
     							CheckingPlayer[playerid] = -1;
							    SendClientMessage(playerid, 0x00b3b3AA, "ERROR: Player seems like didn't turn on the application to get his files!");
							}
			            }
			            else
			            {
			                DirectoryTCP[playerid][0] = EOS;
     						CheckingPlayer[playerid] = -1;
			                SendClientMessage(playerid, 0xff0000AA, "We're not on your damn pc to run applications :|");
			            }
					}
				}
			}
	    }
	    else
	    {
	        DirectoryTCP[playerid][0] = EOS;
	        CheckingPlayer[playerid] = -1;
	    }
	}
	return 0;
}

findlastchar(const destz[], characher)
{
	new len = strlen(destz);
	for(new i = ( len - 1 ); i > -1; i--)
	{
 		if(destz[i] == characher)
   		{
     		return i;
	    }
	}
	return -1;
}

public onSocketRemoteConnect(Socket:id, remote_client[], remote_clientid)
{
    get_remote_client_ip(id, remote_clientid, SocketIP[remote_clientid]);
    ConnectedTCP[remote_clientid] = true;
    TCPTimerID[remote_clientid] = gettime();

}
