using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

struct survey_info
{
    public bool user_submission;
    public string name;
    public string nick;
    public int survey_number;
    public int current_question;
    public int survey_id;
}

namespace IRCBot.Modules
{
    class survey : Module
    {

        List<survey_info> active_surveys = new List<survey_info>();

        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
            {
                foreach (List<string> tmp_command in conf.command_list)
                {
                    if (module_name.Equals(tmp_command[0]))
                    {
                        string[] triggers = tmp_command[3].Split('|');
                        int command_access = Convert.ToInt32(tmp_command[5]);
                        string[] blacklist = tmp_command[6].Split(',');
                        bool blocked = false;
                        bool cmd_found = false;
                        bool spam_check = Convert.ToBoolean(tmp_command[8]);
                        foreach (string bl_chan in blacklist)
                        {
                            if (bl_chan.Equals(channel))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (spam_check == true)
                        {
                            if (ircbot.spam_activated == true)
                            {
                                blocked = true;
                            }
                        }
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(command))
                            {
                                cmd_found = true;
                                break;
                            }
                        }
                        if (blocked == false && cmd_found == true)
                        {
                            foreach (string trigger in triggers)
                            {
                                switch (trigger)
                                {
                                    case "survey":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                int num_survey = 1;
                                                try
                                                {
                                                    num_survey = Convert.ToInt32(line[4].Trim());
                                                    start_survey(nick, nick_access, num_survey - 1, ircbot, conf);
                                                }
                                                catch (Exception ex)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "nextquestion":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            continue_survey(nick, nick_access, ircbot, conf);
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "finishsurvey":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            foreach (survey_info survey in active_surveys)
                                            {
                                                if (nick.Equals(survey.nick) && survey.user_submission == false)
                                                {
                                                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                                                    {
                                                        FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                                                        DirectoryInfo di = fi.Directory;
                                                        FileSystemInfo[] fsi = di.GetFiles();
                                                        if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey.survey_number)
                                                        {
                                                            string[] questions = File.ReadAllLines(fsi[survey.survey_number].FullName);
                                                            if (questions.GetUpperBound(0) > 2)
                                                            {
                                                                ircbot.sendData("PRIVMSG", nick + " :Thank you for submitting the survey.  It is survey #" + (survey.survey_number + 1).ToString());
                                                                active_surveys.RemoveAt(index);
                                                            }
                                                            else
                                                            {
                                                                ircbot.sendData("PRIVMSG", nick + " :You need to submit at least one question for your survey.");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", nick + " :The survey no longer exists.  Please retry adding your survey.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", nick + " :The survey no longer exists.  Please retry adding your survey.");
                                                    }
                                                    break;
                                                }
                                                index++;
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "cancelsurvey":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            foreach (survey_info survey in active_surveys)
                                            {
                                                if (nick.Equals(survey.nick) && survey.user_submission == false)
                                                {
                                                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                                                    {
                                                        FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                                                        DirectoryInfo di = fi.Directory;
                                                        FileSystemInfo[] fsi = di.GetFiles();
                                                        if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey.survey_number)
                                                        {
                                                            File.Delete(fsi[survey.survey_number].FullName);
                                                            string[] owners = File.ReadAllLines(fsi[survey.survey_number].FullName)[2].Split(',');
                                                            foreach (string owner in owners)
                                                            {
                                                                ircbot.sendData("NOTICE", owner + " :" + nick + " has canceled your survey, " + File.ReadAllLines(fsi[survey.survey_number].FullName)[1]);
                                                            }
                                                            active_surveys.RemoveAt(index);
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :The survey does not exist.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :The survey does not exist.");
                                                    }
                                                    break;
                                                }
                                                else if(nick.Equals(survey.nick) && survey.user_submission == true)
                                                {
                                                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + survey.name + Path.DirectorySeparatorChar + nick))
                                                    {
                                                        Directory.Delete(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + survey.name + Path.DirectorySeparatorChar + nick, true);
                                                        active_surveys.RemoveAt(index);
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :The survey does not exist.");
                                                    }
                                                    break;
                                                }
                                                index++;
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "surveys":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                int num_survey = 1;
                                                char[] sep = new char[] { ' ' };
                                                string[] new_line = line[4].Split(sep, StringSplitOptions.RemoveEmptyEntries);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    try
                                                    {
                                                        num_survey = Convert.ToInt32(new_line[0].Trim());
                                                        view_survey(num_survey - 1, nick_access, nick, new_line[1].ToLower(), ircbot, conf);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                                    };
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        num_survey = Convert.ToInt32(new_line[0].Trim());
                                                        view_survey(num_survey - 1, nick_access, nick, null, ircbot, conf);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                view_survey(-1, nick_access, nick, null, ircbot, conf);
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addsurveyowner":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                int num_survey = 1;
                                                string[] new_line = line[4].Split(' ');
                                                try
                                                {
                                                    num_survey = Convert.ToInt32(new_line[0]);
                                                    add_survey_owner(num_survey - 1, nick, new_line[1], ircbot, conf);
                                                }
                                                catch (Exception ex)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delsurveyowner":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                int num_survey = 1;
                                                string[] new_line = line[4].Split(' ');
                                                try
                                                {
                                                    num_survey = Convert.ToInt32(new_line[0]);
                                                    del_survey_owner(num_survey - 1, nick, new_line[1], ircbot, conf);
                                                }
                                                catch (Exception ex)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addsurvey":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                char[] charSep = new char[] { ' ' };
                                                string[] new_line = line[4].Split(charSep, 2);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    int survey_access = conf.user_level - 1;
                                                    bool access_valid = true;
                                                    try
                                                    {
                                                        survey_access = Convert.ToInt32(new_line[0]);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        access_valid = false;
                                                        ircbot.sendData("NOTICE", nick + " :Please choose a valid access level.");
                                                    }
                                                    if (access_valid == true)
                                                    {
                                                        add_survey(nick, survey_access, new_line[1], ircbot, conf);
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :Please include the survey access level and title for the survey.  Ex: " + conf.command + "addsurvey 1 Title of Survey");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :Please include the survey access level and title for the survey.  Ex: " + conf.command + "addsurvey 1 Title of Survey");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :Please include the survey access level and title for the survey.  Ex: " + conf.command + "addsurvey 1 Title of Survey");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delsurvey":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                int num_survey = 1;
                                                try
                                                {
                                                    num_survey = Convert.ToInt32(line[4].Trim());
                                                    del_survey(nick, num_survey - 1, ircbot, conf);
                                                }
                                                catch (Exception ex)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You need to choose a valid survey.  To view all surveys, please type " + conf.command + "surveys");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            if (type.Equals("query") && bot_command == false)
            {
                foreach (survey_info survey in active_surveys)
                {
                    if (nick.Equals(survey.nick))
                    {
                        string answer = "";
                        if (line.GetUpperBound(0) > 3)
                        {
                            answer = line[3].Remove(0,1) + " " + line[4] + Environment.NewLine;
                        }
                        else
                        {
                            answer = line[3].Remove(0, 1) + Environment.NewLine;
                        }
                        if (survey.user_submission == true)
                        {
                            if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + survey.name + Path.DirectorySeparatorChar + survey.nick + Path.DirectorySeparatorChar + ""))
                            {
                                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + survey.name + Path.DirectorySeparatorChar + survey.nick + Path.DirectorySeparatorChar + "");
                            }
                            File.AppendAllText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + survey.name + Path.DirectorySeparatorChar + survey.nick + " " + Path.DirectorySeparatorChar + survey.current_question + ".txt", answer);
                        }
                        else
                        {
                            if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                            {
                                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                            }
                            File.AppendAllText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + survey.name + ".txt", answer);
                        }
                        break;
                    }
                }
            }
        }

        private void view_survey(int survey_num, int nick_access, string nick, string requested_nick, bot ircbot, IRCConfig conf)
        {
            bool survey_found = false;
            if (survey_num != -1)
            {
                if (requested_nick == null)
                {
                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                    {
                        FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                        DirectoryInfo di = fi.Directory;
                        FileSystemInfo[] fsi = di.GetFiles();
                        if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey_num)
                        {
                            string[] questions = File.ReadAllLines(fsi[survey_num].FullName);
                            if (questions.GetUpperBound(0) > 2)
                            {
                                string[] owners = questions[2].Split(',');
                                bool survey_owner = false;
                                foreach (string owner in owners)
                                {
                                    if (nick.Equals(owner))
                                    {
                                        survey_owner = true;
                                        break;
                                    }
                                }
                                if (survey_owner == true)
                                {
                                    survey_found = true;
                                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4) + Path.DirectorySeparatorChar + ""))
                                    {
                                        ircbot.sendData("NOTICE", nick + " :The following nicks have completed or are taking your survey, \"" + questions[1] + "\":");
                                        string nicks = "";
                                        foreach (string dir in Directory.GetDirectories(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4) + Path.DirectorySeparatorChar + ""))
                                        {
                                            nicks += "," + dir.Replace(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4) + Path.DirectorySeparatorChar + "", "");
                                        }
                                        ircbot.sendData("NOTICE", nick + " :" + nicks.TrimStart(','));
                                    }
                                    else
                                    {
                                        ircbot.sendData("NOTICE", nick + " :No nicks have taken that survey.");
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                    {
                        FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                        DirectoryInfo di = fi.Directory;
                        FileSystemInfo[] fsi = di.GetFiles();
                        if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey_num)
                        {
                            string[] questions = File.ReadAllLines(fsi[survey_num].FullName);
                            if (questions.GetUpperBound(0) > 2)
                            {
                                string[] owners = questions[2].Split(',');
                                bool survey_owner = false;
                                foreach (string owner in owners)
                                {
                                    if (nick.Equals(owner))
                                    {
                                        survey_owner = true;
                                        break;
                                    }
                                }
                                if (survey_owner == true)
                                {
                                    survey_found = true;
                                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4) + Path.DirectorySeparatorChar + requested_nick + Path.DirectorySeparatorChar + ""))
                                    {
                                        ircbot.sendData("PRIVMSG", nick + " :" + nick + " has supplied the following answers for your survey, \"" + questions[1] + "\":");
                                        string[] answers = Directory.GetFiles(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4) + Path.DirectorySeparatorChar + requested_nick + Path.DirectorySeparatorChar + "");
                                        int question_num = 0;
                                        for (int x = 3; x <= questions.GetUpperBound(0); x++)
                                        {
                                            ircbot.sendData("PRIVMSG", nick + " :\u0002" + questions[x]);
                                            foreach (string line in File.ReadAllLines(answers[question_num]))
                                            {
                                                ircbot.sendData("PRIVMSG", nick + " :" + line);
                                            }
                                            question_num++;
                                        }
                                    }
                                    else
                                    {
                                        ircbot.sendData("NOTICE", nick + " :" + requested_nick + " has not taken that survey.");
                                    }
                                }
                            }

                        }
                    }
                }
                if (survey_found == false)
                {
                    ircbot.sendData("NOTICE", nick + " :Sorry, but either you are not the owner of the survey, or the survey does not exist.  To view all surveys available to you, please type " + conf.command + "surveys");
                }
            }
            else
            {
                if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                {
                    FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                    DirectoryInfo di = fi.Directory;
                    FileSystemInfo[] fsi = di.GetFiles();
                    int file_number = 1;
                    foreach (FileSystemInfo survey_file in fsi)
                    {
                        string[] questions = File.ReadAllLines(survey_file.FullName);
                        if (questions.GetUpperBound(0) > 1)
                        {
                            int survey_access = Convert.ToInt32(questions[0]);
                            if (survey_access <= nick_access)
                            {
                                survey_found = true;
                                ircbot.sendData("NOTICE", nick + " :" + file_number.ToString() + ") " + questions[1]);
                            }
                            file_number++;
                        }

                    }
                }
                if (survey_found == false)
                {
                    ircbot.sendData("NOTICE", nick + " :There are no surveys available to you.");
                }
            }
        }

        private void add_survey_owner(int survey_num, string nick, string add_owner, bot ircbot, IRCConfig conf)
        {
            bool survey_found = false;
            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
            {
                FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                DirectoryInfo di = fi.Directory;
                FileSystemInfo[] fsi = di.GetFiles();
                if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey_num)
                {
                    string[] questions = File.ReadAllLines(fsi[survey_num].FullName);
                    if (questions.GetUpperBound(0) > 2)
                    {
                        string owners = questions[2];
                        foreach (string owner in owners.Split(','))
                        {
                            if (nick.Equals(owner))
                            {
                                survey_found = true;
                                break;
                            }
                        }
                        if (survey_found == true)
                        {
                            questions[2] = questions[2] + "," + add_owner;
                            StreamWriter sw = new StreamWriter(fsi[survey_num].FullName);
                            foreach (string line in questions)
                            {
                                sw.Write(line + Environment.NewLine);
                            }
                            sw.Close();
                            ircbot.sendData("NOTICE", nick + " :Owner added successfully");
                        }
                        else
                        {
                            survey_found = true;
                            ircbot.sendData("NOTICE", nick + " :You do not have permission to edit this survey.");
                        }
                    }

                }
            }
            if (survey_found == false)
            {
                ircbot.sendData("NOTICE", nick + " :The specified survey does not exist.");
            }
        }

        private void del_survey_owner(int survey_num, string nick, string del_owner, bot ircbot, IRCConfig conf)
        {
            bool survey_found = false;
            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
            {
                FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                DirectoryInfo di = fi.Directory;
                FileSystemInfo[] fsi = di.GetFiles();
                if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey_num)
                {
                    string[] questions = File.ReadAllLines(fsi[survey_num].FullName);
                    if (questions.GetUpperBound(0) > 2)
                    {
                        string owners = questions[2];
                        foreach (string owner in owners.Split(','))
                        {
                            if (nick.Equals(owner))
                            {
                                survey_found = true;
                                break;
                            }
                        }
                        if (survey_found == true)
                        {
                            bool owner_found = false;
                            string new_owners = "";
                            foreach (string owner in owners.Split(','))
                            {
                                bool owner_loop = false;
                                foreach (string old_owner in del_owner.Split(','))
                                {
                                    if (del_owner.Equals(owner))
                                    {
                                        owner_loop = true;
                                        owner_found = true;
                                    }
                                }
                                if (owner_loop == false)
                                {
                                    new_owners = new_owners + "," + owner;
                                }
                            }
                            if (owner_found == true)
                            {
                                questions[2] = new_owners.TrimStart(',');
                                StreamWriter sw = new StreamWriter(fsi[survey_num].FullName);
                                foreach (string line in questions)
                                {
                                    sw.Write(line + Environment.NewLine);
                                }
                                sw.Close();
                                ircbot.sendData("NOTICE", nick + " :Owner deleted successfully");
                            }
                            else
                            {
                                ircbot.sendData("NOTICE", nick + " :Owner was not found");
                            }
                        }
                        else
                        {
                            survey_found = true;
                            ircbot.sendData("NOTICE", nick + " :You do not have permission to edit this survey.");
                        }
                    }

                }
            }
            if (survey_found == false)
            {
                ircbot.sendData("NOTICE", nick + " :The specified survey does not exist.");
            }
        }

        private void add_survey(string nick, int survey_access, string survey_name, bot ircbot, IRCConfig conf)
        {
            bool survey_found = false;
            
            foreach (survey_info survey in active_surveys)
            {
                if (nick.Equals(survey.nick))
                {
                    survey_found = true;
                    break;
                }
            }
            if (survey_found == false)
            {
                if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                {
                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                }

                FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                DirectoryInfo di = fi.Directory;
                FileSystemInfo[] fsi = di.GetFiles();
                File.AppendAllText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "survey_" + (fsi.GetUpperBound(0) + 1).ToString() + ".txt", survey_access.ToString() + Environment.NewLine);
                File.AppendAllText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "survey_" + (fsi.GetUpperBound(0) + 1).ToString() + ".txt", survey_name + Environment.NewLine);
                File.AppendAllText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "survey_" + (fsi.GetUpperBound(0) + 1).ToString() + ".txt", nick + Environment.NewLine);

                survey_info tmp_info = new survey_info();
                tmp_info.name = "survey_" + (fsi.GetUpperBound(0) + 1).ToString();
                tmp_info.nick = nick;
                tmp_info.survey_number = (fsi.GetUpperBound(0) + 1);
                tmp_info.current_question = 1;
                tmp_info.user_submission = false;

                active_surveys.Add(tmp_info);

                ircbot.sendData("PRIVMSG", nick + " :Please type out a question per line.  Once you are finished adding all the questions you want, type " + conf.command + "finishsurvey to submit the finished survey.");
                ircbot.sendData("PRIVMSG", nick + " :If at any time during the survey you wish to cancel, type " + conf.command + "cancelsurvey to cancel your current survey submission.");
            }
            else
            {
                ircbot.sendData("PRIVMSG", nick + " :You already have a survey active.  Please finish the current survey or cancel it to create a new survey.");
            }
        }

        private void del_survey(string nick, int survey_num, bot ircbot, IRCConfig conf)
        {
            int current_survey = 0;
            List<survey_info> tmp_surveys = new List<survey_info>();
            tmp_surveys = active_surveys;
            foreach (survey_info survey in tmp_surveys)
            {
                if (survey_num == survey.survey_number)
                {
                    ircbot.sendData("NOTICE", survey.nick + " :Sorry, but the survey you are currently taking has been deleted.");
                    active_surveys.RemoveAt(current_survey);
                }
                else
                {
                    current_survey++;
                }
            }

            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
            {
                FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                DirectoryInfo di = fi.Directory;
                FileSystemInfo[] fsi = di.GetFiles();
                if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey_num)
                {
                    Directory.Delete(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4) + Path.DirectorySeparatorChar + "", true);
                    File.Delete(fsi[survey_num].FullName);
                    ircbot.sendData("NOTICE", nick + " :Survey deleted successfully");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :Survey not found");
                }
            }
        }

        private void continue_survey(string nick, int nick_access, bot ircbot, IRCConfig conf)
        {
            int cur_survey = 0;
            List<survey_info> new_survey = new List<survey_info>();
            new_survey = active_surveys;
            foreach (survey_info tmp_survey in new_survey)
            {
                if (nick.Equals(tmp_survey.nick))
                {
                    survey_info this_survey = active_surveys[cur_survey];
                    FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                    DirectoryInfo di = fi.Directory;
                    FileSystemInfo[] fsi = di.GetFiles();
                    bool survey_found = false;
                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
                    {
                        if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= tmp_survey.survey_number)
                        {
                            string[] questions = File.ReadAllLines(fsi[tmp_survey.survey_number].FullName);
                            if (questions.GetUpperBound(0) > 2)
                            {
                                if (Convert.ToInt32(questions[0]) <= nick_access)
                                {
                                    survey_found = true;
                                    this_survey.current_question = tmp_survey.current_question + 1;
                                    active_surveys[cur_survey] = this_survey;
                                    if (questions.GetUpperBound(0) > this_survey.current_question + 1)
                                    {
                                        if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_survey.name + Path.DirectorySeparatorChar + tmp_survey.nick + Path.DirectorySeparatorChar + ""))
                                        {
                                            Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_survey.name + Path.DirectorySeparatorChar + tmp_survey.nick + Path.DirectorySeparatorChar + "");
                                        }

                                        if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_survey.name + Path.DirectorySeparatorChar + tmp_survey.nick + Path.DirectorySeparatorChar + this_survey.current_question + ".txt"))
                                        {
                                            File.Delete(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_survey.name + Path.DirectorySeparatorChar + tmp_survey.nick + Path.DirectorySeparatorChar + this_survey.current_question + ".txt");
                                            File.Create(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_survey.name + Path.DirectorySeparatorChar + tmp_survey.nick + Path.DirectorySeparatorChar + this_survey.current_question + ".txt");
                                        }
                                        else
                                        {
                                            File.Create(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + "survey_" + tmp_survey.survey_number + Path.DirectorySeparatorChar + tmp_survey.nick + Path.DirectorySeparatorChar + this_survey.current_question + ".txt");
                                        }

                                        ircbot.sendData("PRIVMSG", nick + " :" + questions[this_survey.current_question + 2]);
                                    }
                                    else
                                    {
                                        ircbot.sendData("PRIVMSG", nick + " :Thank you for completing the survey!  Your answers have been recorded and sent to the survey owner.");

                                        string[] owners = questions[2].Split(',');
                                        foreach (string owner in owners)
                                        {
                                            ircbot.sendData("PRIVMSG", owner + " :" + nick + " has finished your survey, \"" + questions[1] + "\"");
                                            ircbot.sendData("PRIVMSG", owner + " :To view his answers, please type " + conf.command + "surveys " + (tmp_survey.survey_number + 1).ToString() + " " + tmp_survey.nick);
                                        }
                                        active_surveys.RemoveAt(cur_survey);
                                    }
                                }
                            }
                        }
                    }
                    if (survey_found == false)
                    {
                        ircbot.sendData("PRIVMSG", nick + " :Sorry, but that survey is not available to you anymore.  To view all surveys available to you, please type .surveys");
                        active_surveys.RemoveAt(cur_survey);
                    }
                    break;
                }
                cur_survey++;
            }
        }

        private void start_survey(string nick, int nick_access, int survey_num, bot ircbot, IRCConfig conf)
        {
            bool survey_found = false;
            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + ""))
            {
                FileInfo fi = new FileInfo(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "surveys" + Path.DirectorySeparatorChar + "");
                DirectoryInfo di = fi.Directory;
                FileSystemInfo[] fsi = di.GetFiles();
                if (fsi.GetUpperBound(0) >= 0 && fsi.GetUpperBound(0) >= survey_num)
                {
                    foreach (survey_info survey in active_surveys)
                    {
                        if (nick.Equals(survey.nick))
                        {
                            survey_found = true;
                            break;
                        }
                    }
                    if (survey_found == false)
                    {
                        string[] questions = File.ReadAllLines(fsi[survey_num].FullName);
                        if (questions.GetUpperBound(0) > 2)
                        {
                            if (Convert.ToInt32(questions[0]) <= nick_access)
                            {
                                survey_found = true;
                                survey_info tmp_info = new survey_info();
                                tmp_info.user_submission = true;
                                tmp_info.name = fsi[survey_num].Name.Substring(0, fsi[survey_num].Name.Length - 4);
                                tmp_info.nick = nick;
                                tmp_info.survey_number = survey_num;
                                tmp_info.current_question = 1;
                                tmp_info.survey_id = survey_num;

                                active_surveys.Add(tmp_info);

                                if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_info.name + Path.DirectorySeparatorChar + tmp_info.nick + Path.DirectorySeparatorChar + ""))
                                {
                                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_info.name + Path.DirectorySeparatorChar + tmp_info.nick + Path.DirectorySeparatorChar + "");
                                }

                                if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_info.name + Path.DirectorySeparatorChar + tmp_info.nick + Path.DirectorySeparatorChar + tmp_info.current_question + ".txt"))
                                {
                                    File.Delete(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_info.name + Path.DirectorySeparatorChar + tmp_info.nick + Path.DirectorySeparatorChar + tmp_info.current_question + ".txt");
                                    File.Create(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + tmp_info.name + Path.DirectorySeparatorChar + tmp_info.nick + Path.DirectorySeparatorChar + tmp_info.current_question + ".txt");
                                }
                                else
                                {
                                    File.Create(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "survey" + Path.DirectorySeparatorChar + "answers" + Path.DirectorySeparatorChar + "survey_" + tmp_info.survey_number + Path.DirectorySeparatorChar + tmp_info.nick + Path.DirectorySeparatorChar + tmp_info.current_question + ".txt");
                                }

                                ircbot.sendData("PRIVMSG", nick + " :You have chosen to take the following survey: " + questions[1]);
                                ircbot.sendData("PRIVMSG", nick + " :You will be presented with a series of questions.  After you write the answer, type " + conf.command + "nextquestion to submit your answer and view the next question.");
                                ircbot.sendData("PRIVMSG", nick + " :If at any time during the survey you wish to cancel, type " + conf.command + "cancelsurvey to cancel your current survey and any answers you may have submitted.");
                                string[] owners = questions[2].Split(',');
                                foreach (string owner in owners)
                                {
                                    ircbot.sendData("NOTICE", owner + " :" + nick + " has started your survey, \"" + questions[1] + "\"");
                                }
                                ircbot.sendData("PRIVMSG", nick + " :" + questions[3]);
                            }
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You are already taking a survey.  Please finish the current survey or cancel it to choose a new survey.");
                    }
                }
            }
            if (survey_found == false)
            {
                ircbot.sendData("NOTICE", nick + " :Sorry, but that survey is not available to you.  To view all surveys available to you, please type " + conf.command + "surveys");
            }
        }
    }
}
