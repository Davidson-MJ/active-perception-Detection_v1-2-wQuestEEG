% Detection experiment (contrast)
%%  Import from csv. FramebyFrame, then summary data.

%%%%%% v3: QUEST w Eye and EEG version %%%%%%
%frame by frame first:
%Mac:
% datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
%PC:d
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-2-wQuestEEG\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';

cd ../Processed_Data
procdatadir = pwd; 

cd(datadir)
pfols = dir([pwd filesep '*framebyframe.csv']);
nsubs= length(pfols);

% show ppant numbers: in command window
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)
%% Per csv file, import and wrangle into Matlab Structures, and data matrices:
for ippant =1%1:nsubs
    cd(datadir)
    
    pfols = dir([pwd filesep '*framebyframe.csv']);

    %% load subject data as table.
    filename = pfols(ippant).name;
    %extract name&date from filename:
    ftmp = find(filename =='_');
    subjID = filename(1:ftmp(end)-1);
    
   
    savename = [subjID '_summary_data'];
 
    %query whether we want to recompute frame x frame (unlikely).
    cd(procdatadir);
    if exist(savename, 'file')
        disp(['frame x frame alread saved for ' subjID]);
    else
    %read table
    cd(datadir);
    opts = detectImportOptions(filename,'NumHeaderLines',0);
    disp(['reading large frame x frame file now...']);
    T = readtable(filename,opts);
    ppant = T.participant{1};
    disp(['Preparing participant ' ppant]);
    
    [TargPos, HeadPos, TargState, ClickState, EyePos, EyeDir] = deal([]);
    
    %% use logical indexing to find all relevant info (in cells)
    posData = T.position;
    clickData = T.clickstate;
    targStateData= T.targState;
  
    objs = T.trackedObject;
    axes= T.axis;
    Trials =T.trial;
    Times = T.t;
    
    targ_rows = find(contains(objs, 'target'));
    head_rows = find(contains(objs, 'head'));
    eyePos_rows = find(contains(objs, 'gazeOrigin'));   
    eyeDir_rows = find(contains(objs, 'gazeDirection'));
   
    Xpos = find(contains(axes, 'x'));
    Ypos  = find(contains(axes, 'y'));
    Zpos = find(contains(axes, 'z'));
    
    userows = {head_rows, targ_rows, eyePos_rows, eyeDir_rows};
    for idatatype = 1:length(userows)
        
        %% per type, find the intersect of thse indices, to fill the data.
        datarows = userows{idatatype};
        Dx = intersect(datarows, Xpos);
        Dy = intersect(datarows, Ypos);
        Dz = intersect(datarows, Zpos);
        
        %% further store by trials (walking laps).
        vec_lengths=[];
        
        DataPos=[]; % will be renamed below.
        
        for itrial = 1:length(unique(Trials))
            
            trial_rows = find(Trials==itrial-1); % indexing from 0 in Unity
            
            DataPos(itrial).X = posData(intersect(Dx, trial_rows));
            DataPos(itrial).Y = posData(intersect(Dy, trial_rows));
            DataPos(itrial).Z = posData(intersect(Dz, trial_rows));
            
            % only need to perform once, but also capture the targstate and
            % click state, and times on each trial
            
            trial_times = Times(intersect(Dx, trial_rows));
            
            if idatatype==1
                trialInfo(itrial).targstate= targStateData(intersect(Dx, trial_rows));
                trialInfo(itrial).clickstate= clickData(intersect(Dx, trial_rows));
                trialInfo(itrial).times = trial_times;                
            end
        end
        
        % save per datatype:
        switch idatatype
            case 1
                HeadPos = DataPos;
            case 2
                TargPos = DataPos;
            case 3
                EyePos = DataPos;
            case 4
                EyeDir= DataPos;
                
        end
    end % idatatype
    
    disp(['Saving position data split by trials... ' subjID]);
    cd(procdatadir)
    try save(savename, 'TargPos', 'HeadPos', 'EyePos', 'EyeDir', 'TargState', 'clickState', 'subjID', 'ppant', '-append');
    catch
        save(savename, 'TargPos', 'HeadPos', 'EyePos', 'EyeDir','TargState', 'ClickState', 'subjID', 'ppant');
    end
  
    end
%
%% ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ now summary data
  cd(datadir)
pfols = dir([pwd filesep '*trialsummary.csv']);
nsubs= length(pfols);
%
filename = pfols(ippant).name;
  
    %extract name&date from filename:
    ftmp = find(filename =='_');
    subjID = filename(1:ftmp(end)-1);
    %read table
    opts = detectImportOptions(filename,'NumHeaderLines',0);
    T = readtable(filename,opts);
    rawSummary_table = T;
    disp(['Preparing participant ' T.participant{1} ]);
    
    
    savename = [subjID '_summary_data'];
    % summarise relevant data:
    targPrestrials =find(T.nTarg>0);
    practIndex = find(T.isPrac ==1);
    npracTrials = (T.trial(practIndex(end)) +1);
    disp([subjID ' has ' num2str(npracTrials) ' practice trials']);
    
    %extract the rows in our table, with relevant data for assessing
    %calibration
    nstairs = unique(T.qStaircase(T.qStaircase >0));
    calibAcc=[];
    for iqstair=1:length(nstairs) % check all staircases.
        tmpstair = nstairs(iqstair);
        qstairtrials= find(T.qStaircase==tmpstair);
        calibAxis = intersect(qstairtrials, practIndex);
        
        %calculate accuracy (running accuracy).
        calibData = T.targCor(calibAxis);        
        for itarg=1:length(calibData)
            tmpD = calibData(1:itarg);
            calibAcc(iqstair).d(itarg) = sum(tmpD)/length(tmpD);
        end
       
    end
    
    %% FAlse alarms: repair FA data in table (happens if we have multiple FAs, extra columns are added, which need to be collapsed.
    ColIndex = find(strcmp(T.Properties.VariableNames, 'FA_rt'), 1);
    %repair string columns:
    for ix=ColIndex:size(T,2)
        tmp = table2array(T(:, ix));
        if iscell(tmp)
            % where is there non-zero data, to convert?
            replaceRows= find(~cellfun(@isempty,tmp));
            %convert each row
            newD=zeros(size(tmp,1),1);            
            for ir=1:length(replaceRows)
                user = replaceRows(ir);
                newD(user) = str2num(cell2mat(tmp(user )));
            end
            
            %% change table column type:
            colnam = T.Properties.VariableNames(ix);
            T.(colnam{:}) = newD;
%             T(:,ix) =table(newD');
            
        end
        
        
    end
    %% extract Target onsets per trial (as struct).
    %% and Targ RTs, contrast values if they exist.
    alltrials = unique(T.trial);
    trial_TargetSummary=[];
    
    for itrial= 1:length(alltrials)
        thistrial= alltrials(itrial);
        relvrows = find(T.trial ==thistrial); % unity idx at zero.
        
        %create vectors for storage:
        tOnsets = T.targOnset(relvrows);
        tRTs = T.targRT(relvrows);
        tCor = T.targCor(relvrows);
        tFAs= table2array(T(relvrows(end), ColIndex:end));       
        tFAs= tFAs(~isnan(tFAs));
        
        
        % seems some FA are missing, do a quick check to see if 
         % any extra in the clickdata.
        clks= find(ClickState(itrial).state);
        clksTs= DataPos(itrial).times(clks);
         if length(clksTs) ~= length(tRTs) 
             %FA present
             %find the outlier
             %round times to 3dp:
             clksTsR= round(clksTs,3);
             % find out which is furthest from a recorded click
             % in the sumry data. .:. a FA:
             [loc, dist] =dsearchn(tRTs, clksTsR);
             FAidx= find(dist > .1);
             for iextraclick = 1:length(FAidx)
                 thisFA_isat = clksTsR(FAidx(iextraclick));
                 
                 % note, if this FA isn't already recorded,
                 % store
                 if ~isempty(tFAs)
                     [~, distfromrecd] = dsearchn(tFAs', thisFA_isat);
                     if distfromrecd >.05 % another click
                         tFAs= [tFAs, thisFA_isat];
                     end
                     
                 else % first FA
                     tFAs =thisFA_isat;
                 end
             end
             %sanity debug:
%              clf; plot(HeadPos(itrial).times, HeadPos(itrial).Y);
%              yyaxis right
%              plot(HeadPos(itrial).times, TargState(itrial).state); hold on;
%              plot(HeadPos(itrial).times, clickState(itrial).state);
             
         end
        %contrast per targ
        tContr = T.targContrast(relvrows); 
        
        
            %also convert to index (in range 1:7).
        contrastIndex = dsearchn(contrastValues, tContr);
        
        
        RTs = (tRTs - tOnsets);        
%         %note that negative RTs, indicate that no response was recorded:
        tOmit = find(RTs<=0);
        if ~isempty(tOmit)
%             tCor(tOmit) = NaN; % don't count those incorrects, as a mis identification.
            RTs(tOmit)=NaN; % remove no respnse 
        end
        
        
        % we also want to reject very short RTs (reclassify as a FA).
        
        if any(find(RTs<0.15))
            disp(['Suspicious RT']);
            checkRT= find(RTs<.15);
            %debug to check:
%           
%              clf; plot(HeadPos(itrial).times, HeadPos(itrial).Y);
%              yyaxis right
%              plot(HeadPos(itrial).times, TargState(itrial).state); hold on;
%              plot(HeadPos(itrial).times, clickState(itrial).state);
%              
            % reclassify data.
            for ifa= 1:length(checkRT)
                % not actually a correct response:
                tCor(ifa)=0;
                RTs(ifa)= NaN; % no response to that target
                
                %add as a FA
                tFAs= [tFAs, tRTs(ifa)];
                
                %rmv from clicks recorded
                tRTs(ifa)=0;
            end
            
        end
        %store in easier to wrangle format
        trial_TargetSummary(itrial).trialID= thistrial;
        trial_TargetSummary(itrial).targOnsets= tOnsets;
        trial_TargetSummary(itrial).targContrast= tContr;
        trial_TargetSummary(itrial).targContrastIndx= contrastIndex;
        
        trial_TargetSummary(itrial).targdetected= tCor;
        trial_TargetSummary(itrial).targRTs= RTs;
        trial_TargetSummary(itrial).clickOnsets= tRTs;
       
        trial_TargetSummary(itrial).FalseAlarms= tFAs;
        
        trial_TargetSummary(itrial).isPrac= DataPos(itrial).isPrac;        
        trial_TargetSummary(itrial).isStationary= DataPos(itrial).isStationary;
        
          %also convert to index (in range 1:7).
      
    end
     
    %save for later analysis per gait-cycle:
    disp(['Saving trial summary data ... ' subjID]);
    rawdata_table = T;
    cd('ProcessedData')
    save(savename, 'trial_TargetSummary', 'calibContrast', 'calibAcc', 'calibData',...
        'rawdata_table', 'subjID','rawSummary_table', '-append');
    

end % participant
