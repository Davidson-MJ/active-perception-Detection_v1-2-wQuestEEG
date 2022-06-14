% Detection experiment (contrast)
%%  Import from csv. FramebyFrame, then summary data.

%%%%%% QUEST DETECT version %%%%%%
%frame by frame first:
%Mac:
% datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
%PC:
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';

cd(datadir)
pfols = dir([pwd filesep '*framebyframe.csv']);
nsubs= length(pfols);
% show ppant numbers:

tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)
%% Per csv file, import and wrangle into Matlab Structures, and data matrices:
for ippant =3%1:nsubs
    cd(datadir)
    
    pfols = dir([pwd filesep '*framebyframe.csv']);

    %% load subject data as table.
    filename = pfols(ippant).name;
    %extract name&date from filename:
    ftmp = find(filename =='_');
    subjID = filename(1:ftmp(end)-1);
    
   
    savename = [subjID '_summary_data'];
    
%     %query whether pos data job has been done (is in list of variables
%     %saved)
%     cd('ProcessedData')
%     listOfVariables = who('-file', [savename '.mat']);
%     if ~ismember('HeadPos', listOfVariables)  
%        % if not done, load and save frame x frame data.
%     % simple extract of positions over time.
%     
    %read table
    opts = detectImportOptions(filename,'NumHeaderLines',0);
    T = readtable(filename,opts);
    ppant = T.participant{1};
    disp(['Preparing participant ' ppant]);
    
    [TargPos, HeadPos, TargState, clickState] = deal([]);
    
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
   
    Xpos = find(contains(axes, 'x'));
    Ypos  = find(contains(axes, 'y'));
    Zpos = find(contains(axes, 'z'));
    
    %% now find the intersect of thse indices, to fill the data.
    hx = intersect(head_rows, Xpos);
    hy = intersect(head_rows, Ypos);
    hz = intersect(head_rows, Zpos);
    
    %Targ (XYZ)
    tx = intersect(targ_rows, Xpos);
    ty = intersect(targ_rows, Ypos);
    tz = intersect(targ_rows, Zpos);
    
    %% further store by trials (walking laps).
    vec_lengths=[];
    for itrial = 1:length(unique(Trials))
        
        trial_rows = find(Trials==itrial-1); % indexing from 0 in Unity
        
        trial_times = Times(intersect(hx, trial_rows));
        %Head first (X Y Z)
        HeadPos(itrial).X = posData(intersect(hx, trial_rows));
        HeadPos(itrial).Y = posData(intersect(hy, trial_rows));
        HeadPos(itrial).Z = posData(intersect(hz, trial_rows));
        %store time (sec) for matching with summary data:
        HeadPos(itrial).times = trial_times;
        
        HeadPos(itrial).isPrac = unique(T.isPrac(trial_rows));        
        HeadPos(itrial).isStationary = unique(T.isStationary(trial_rows));
        
        
        
        TargPos(itrial).X = posData(intersect(tx, trial_rows));
        TargPos(itrial).Y = posData(intersect(ty, trial_rows));
        TargPos(itrial).Z = posData(intersect(tz, trial_rows));        
         TargPos(itrial).times = trial_times;
        TargPos(itrial).isPrac = unique(T.isPrac(trial_rows));        
        TargPos(itrial).isStationary = unique(T.isStationary(trial_rows));
        
        
        % because the XYZ have the same time stamp, collect click and targ
        % state as well.
        % note varying lengths some trials, so store in structure:
        TargState(itrial).state = targStateData(intersect(hx, trial_rows));
        TargState(itrial).times = trial_times;
        clickState(itrial).state = clickData(intersect(hx, trial_rows));
        clickState(itrial).times = trial_times;
        
        
        
    end
    
    
    disp(['Saving position data split by trials... ' subjID]);
    rawFramedata_table = T;
    cd([datadir filesep 'ProcessedData'])
%     try save(savename, 'TargPos', 'HeadPos', 'TargState', 'clickState', 'subjID', 'ppant', '-append');
%     catch
        save(savename, 'TargPos', 'HeadPos', 'TargState', 'clickState', 'subjID', 'ppant');
%     end
    
%     else
%         disp(['skipping frame by frame save for ' subjID]);
%         % actually need to load the HeadPos and clickState for summary jobs
%         % below:
%         load(savename, 'HeadPos', 'clickState');
%     end % query frame x frame job
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
    %check if contrast info is saved (not the case for some pilots).
    Exist_Column = strcmp('targContrast',T.Properties.VariableNames);
    val = Exist_Column(Exist_Column==1);
    if val
        contrastValues= unique(T.targContrast(practIndex(end)+2:end));
        if length(contrastValues)~=7
            error(['incorret contrast information, post calibration for ' subjID])
        end
        hasContrast=1;
    end
    
    %extract the rows in our table, with relevant data for assessing
    %calibration
    nstairs = unique(T.qStaircase(T.qStaircase >0));
    calibAcc=[];
    calibContrast=[];
    for iqstair=1:length(nstairs) % check all staircases.
        tmpstair = nstairs(iqstair);
        qstairtrials= find(T.qStaircase==tmpstair);
        calibAxis = intersect(qstairtrials, practIndex);
        
        %calculate accuracy:
        calibData = T.targCor(calibAxis);
        
        for itarg=1:length(calibData)
            tmpD = calibData(1:itarg);
            calibAcc(iqstair).d(itarg) = sum(tmpD)/length(tmpD);
        end
        %retain contrast values:
        calibContrast(iqstair).c = T.targContrast(calibAxis);
    end
    
    %% FAlse alarms: repair FA data in table    
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
        clks= find(clickState(itrial).state);
        clksTs= HeadPos(itrial).times(clks);
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
        
        trial_TargetSummary(itrial).isPrac= HeadPos(itrial).isPrac;        
        trial_TargetSummary(itrial).isStationary= HeadPos(itrial).isStationary;
        
          %also convert to index (in range 1:7).
      
    end
     
    %save for later analysis per gait-cycle:
    disp(['Saving trial summary data ... ' subjID]);
    rawdata_table = T;
    cd('ProcessedData')
    save(savename, 'trial_TargetSummary', 'calibContrast', 'calibAcc', 'calibData',...
        'rawdata_table', 'subjID','rawSummary_table', '-append');
    

end % participant
