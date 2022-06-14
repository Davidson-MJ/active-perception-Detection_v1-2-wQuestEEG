% j2_binData_bycycle
% here we will bin trial information into gait cycles.

% a little tricky, as gait cycle duration varies.

% First take: resample time vector of each gait to 100 points, for alignment across
% gaits.
%Mac:
% datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
%PC:
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';


%%%%%% QUEST DETECT version %%%%%%

% this uses the frame-by-frame data, but that introduces problems with FA.
% consider a different script to use trial summary data?


% clear all; close all;
cd([datadir filesep 'ProcessedData'])
pfols= dir([pwd  filesep '*summary_data.mat']);
nsubs= length(pfols);
%show ppant list:
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

%
%%


for ippant =2:nsubs
    cd([datadir filesep 'ProcessedData'])    %%load data from import job.
    load(pfols(ippant).name, ...
        'HeadPos', 'clickState', 'TargState', 'trial_TargetSummary', 'subjID');
    savename = pfols(ippant).name;
    disp(['Preparing j2 cycle data... ' savename]);
    
    %% Gait extraction.
    % Per trial, extract gait samples (trough to trough), normalize along x
    % axis, and store various metrics.
    
    rtcounter=1;
    gaitRTs= []; % will be gPcnt (targ) , RT, H/M
    allgpcnt=[];
    allRpcnt=[];
    for itrial=1:size(HeadPos,2)
        if HeadPos(itrial).isPrac || HeadPos(itrial).isStationary
            continue
        end
        
        
        %% subj specific trial rejection
        skip=0;
        rejTrials_questv1; %toggles 'skip' based on bad trial ID               
        if skip==1
            continue
        end
        %%
        
        trs = HeadPos(itrial).Y_gait_troughs;
        pks = HeadPos(itrial).Y_gait_peaks;
        trialTime = HeadPos(itrial).times;
        
        % plot gaits overlayed;
        
        % Head position data:
        tmpPos=  squeeze(HeadPos(itrial).Y);
        tmpSway = squeeze(HeadPos(itrial).Z);
        tmpwalkDir = squeeze(HeadPos(itrial).X);
        
        % quick classification:
        if mod(itrial,2)~=0 % odd numbers (walking toward bldng)
            % Then descending on X, (i.e. first trajectory), more positive z values
            % are left side of the body.
            walkDir= 'IN';
        elseif mod(itrial,2) ==0
            % ascending (the return trajectory), pos z values are RHS
            walkDir= 'OUT';
        end
            
        % targ onset and click (RT) data:
        tmpTarg = squeeze(TargState(itrial).state);
        tmpClick = squeeze(clickState(itrial).state);
        
        
        %preAllocate for easy storage
        gaitD=[]; %struct
        [gaitHeadY, gaitTarg]= deal(zeros(length(pks), 100)); % we will normalize the vector lengths.
        gaitType= {}; % we'll store the foot centred at each peak.
        
        %summary info for comparison:
        tOns_sumry= trial_TargetSummary(itrial).targOnsets;
        tCor_sumry = trial_TargetSummary(itrial).targdetected;
        tClickOns_smry= trial_TargetSummary(itrial).clickOnsets;
        tRTs_sumry = trial_TargetSummary(itrial).targRTs;
        tFAs_sumry = trial_TargetSummary(itrial).FalseAlarms;
        tContrast_sumry  = trial_TargetSummary(itrial).targContrast;        
        tContrastIDX_sumry  = trial_TargetSummary(itrial).targContrastIndx;
        % what trial type?
        targsPresented = max(tmpTarg);
        
        
        %% store the resampled gait information (rescaling to 100 samples)
        for igait=1:length(pks)
            gaitsamps =[trs(igait):trs(igait+1)-1]; % from trough to frame before next trough.
%             if length(gaitsamps)<40 %%  ~450 ms
%                 continue
%             end
            gaitTimes = HeadPos(itrial).times(gaitsamps);
            % head Y data this gait:
            gaitDtmp = tmpPos(gaitsamps);
            
            %head sway (z) this gait:
            gaitZtmp = tmpSway(gaitsamps);
            
            %which foot starts this gait??
            %% debug plot to sanity check.
            % not I can animate a single trial, with script.
            % plotj2_singletrial_walkgif.m
%             clf; subplot(211); plot(tmpSway);
%             hold on;
%             plot(trs(igait), tmpSway(trs(igait),1), 'color','b', 'marker','o');
%             plot(trs(igait+1), tmpSway(trs(igait+1),1), 'color','k', 'marker','o');
%             if strcmp(walkDir, 'OUT') 
%                 set(gca, 'ydir', 'reverse')
%             end
%             subplot(212); plot(tmpPos);
%             hold on;
%             plot(trs(igait), tmpPos(trs(igait),1), 'color','b', 'marker','o');
%             plot(trs(igait+1), tmpPos(trs(igait+1),1), 'color','k', 'marker','o');
            %%
            % is the z value increasing or decreasing? (sway direction)
            at_trough = tmpSway(trs(igait));
            post_trough = mean(tmpSway(trs(igait):trs(igait)+5));
            
            if at_trough>post_trough% head was closer to the wall, now swinging toward stairwell
                if strcmp(walkDir, 'IN') 
                    gaitD(igait).peak= 'RL'; % shifting weight to right foot.
                else % reverse orintation, increasing numbers swinging to the RHS
                    gaitD(igait).peak= 'LR';
                end
            else % head trough, this step, is closer to the stairs than next step
                 if strcmp(walkDir, 'IN') 
                    % then swinging to the LHS of the body (pushing off
                    % right foot at the previous pk.
                    gaitD(igait).peak= 'LR';
                else % reverse orintation, increasing numbers swinging to the RHS
                    gaitD(igait).peak= 'RL';
                 end
            end
                
            % normalize height between 0 and 1
            gaitDtmp_n = rescale(gaitDtmp);
            
            
            % store data in matrix for easy handling:
            gaitHeadY(igait,:) = imresize(gaitDtmp_n', [1,100]);
            
            
            %also store head Y info:
            gaitD(igait).Head_Yraw = gaitDtmp;
            gaitD(igait).Head_Ynorm = gaitDtmp_n;
            gaitD(igait).Head_Y_resampled = imresize(gaitDtmp_n', [1,100]);
            gaitD(igait).gaitsamps = gaitsamps;
            gaitD(igait).gaitTimes = gaitTimes;
            gaitD(igait).gaitTimes_strt_fin = [gaitTimes(1) gaitTimes(end)];
            % other cycle info:
            %height
            gaitD(igait).tr2pk = tmpPos(pks(igait)) - tmpPos(trs(igait));
            gaitD(igait).pk2tr = tmpPos(pks(igait)) - tmpPos(trs(igait+1));
            
            %dist
            gaitD(igait).tr2pk_dur = length(trs(igait):pks(igait));
            gaitD(igait).pk2tr_dur = length(pks(igait):trs(igait+1));
            
            %height ./ dist
            risespeed = tmpPos(pks(igait)) - tmpPos(trs(igait)) / length(trs(igait):pks(igait));
            fallspeed = tmpPos(pks(igait)) - tmpPos(trs(igait+1)) / length(pks(igait):trs(igait+1));
            
            gaitD(igait).risespeed = risespeed;
            gaitD(igait).fallspeed = fallspeed;
            
        end % resampling gait info (all in trial).
        
        
        
        
        trial_TargetSummary(itrial).gaitHeadY= gaitHeadY;
        trial_TargetSummary(itrial).gaitType = {gaitD(:).peak};
        
        % New! Now, using the trialtarget summary info, find the closest
        % point in each gait cycle, to the events of interest (responses,
        % fA etc).
        
        % previously, we stepped through each gait and searched for
        % responses, but this confused the FA with actual responses. 
        % Instead, now we will use the trial summary as ground truth, and
        % find closest point in each relevant gait.
        
        
        %% determine target onset as % of gait:
        
        for itargO = 1:length(tOns_sumry)
            
            trgIS = tOns_sumry(itargO);
            %step through each gait (the times), and find relevant one containing trg onset.
            for igait = 1:length(gaitD)
                tmptimes= gaitD(igait).gaitTimes;
                % continue if outside range.
                if trgIS< tmptimes(1) || trgIS> tmptimes(end)
                    continue
                end
                
                 if trgIS >=tmptimes(1) && trgIS <= tmptimes(end) % event is within this gait.
                    %find gait pcnt.
                    idx = dsearchn(tmptimes, trgIS);
                    
%                     gPcnt = round((idx/length(tmptimes))*100);                    
                    %alternate way:
                    resizeT= imresize(tmptimes', [1,100]);
                    gPcnt = dsearchn(resizeT', trgIS);
                    
                    %store info using summary and accurate framexframe data:
                    
                    gaitD(igait).tOnset_inTrialTime =  tmptimes(idx);
                    gaitD(igait).tOnset_inGait = idx;
                    gaitD(igait).tOnset_inGaitResampled = gPcnt;
                     gaitD(igait).targRT_fromsmry = tRTs_sumry(itargO);                     
                    % was the resp correct or no? (Hit or Miss).
                    gaitD(igait).tTargDetected= tCor_sumry(itargO);
                    gaitD(igait).tContrast = tContrast_sumry(itargO); % contrast (value)                    
                    gaitD(igait).tContrastIDX = tContrastIDX_sumry(itargO); % contrast (index in range)
                     
                 end
            
            end% gait
        end % targOns
           
        %% now do the same for all recorded, responses this trial.
        for irespO = 1:length(tClickOns_smry)
            
            respIS = tClickOns_smry(irespO);
            
            if respIS==0 % i.e. 0 the placeholder for a 'miss'
                continue
            end
            
            %step through each gait (the times), and find relevant one containing click onset.
            for igait = 1:length(gaitD)
                tmptimes= gaitD(igait).gaitTimes;
                % continue if outside range.
                if respIS < tmptimes(1) || respIS > tmptimes(end)
                    continue
                end
                
                 if respIS >=tmptimes(1) && respIS  <= tmptimes(end) % event is within this gait.
                    %find gait pcnt.
                    idx = dsearchn(tmptimes, respIS );
                    
%                     rPcnt = round((idx/length(tmptimes))*100); %position as pcnt of cycle
                     resizeT= imresize(tmptimes', [1,100]);
                    rPcnt = dsearchn(resizeT', respIS);
                    
                    %store info using summary and accurate framexframe data:
                    
                       gaitD(igait).response_rawsamp = idx;                    
                      gaitD(igait).response_rawtime = tmptimes(idx);                              
                      gaitD(igait).response_rawRT_fromsmry = tRTs_sumry(irespO);
                      gaitD(igait).response_resamp = rPcnt;
                      gaitD(igait).response_corr= tCor_sumry(irespO);
                      
                 end
            
            end% gait
        end % irespO
          %% same for any False Alarms:
          if any(tFAs_sumry >0)
              for iFAO= 1:length(tFAs_sumry)
                  
                  respIS = tFAs_sumry(iFAO);
                  if respIS==0
                      continue
                  end
                      %step through each gait (the times), and find relevant one containing click onset.
                      for igait = 1:length(gaitD)
                          tmptimes= gaitD(igait).gaitTimes;
                          % continue if outside range.
                          if respIS < tmptimes(1) || respIS > tmptimes(end)
                              continue
                          end
                          
                          if respIS >=tmptimes(1) && respIS  <= tmptimes(end) % event is within this gait.
                              %find gait pcnt.
                              idx = dsearchn(tmptimes, respIS );
                              
%                               rPcnt = round((idx/length(tmptimes))*100);
                                resizeT= imresize(tmptimes', [1,100]);
                                rPcnt = dsearchn(resizeT', respIS);

                              %store info using summary and accurate framexframe data:
                              
                              gaitD(igait).FA_rawsamp = idx;
                              gaitD(igait).FA_rawtime = tmptimes(idx);                             
                              gaitD(igait).FA_resamp = rPcnt;
                              
                              
                          end
                      
                  end% gait
              end % iFalse alarm
          end
          
            
        % store array of all gait events, at trial level:
        if targsPresented
            trial_TargetSummary(itrial).gaitTargs_detected = [gaitD(:).tTargDetected];
             trial_TargetSummary(itrial).gaitTargs_RT = [gaitD(:).targRT_fromsmry];
            trial_TargetSummary(itrial).gaitTargOns_resamp = [gaitD(:).tOnset_inGaitResampled];             
        end
        try
            trial_TargetSummary(itrial).gaitResp_resamp = [gaitD(:).response_resamp];
            trial_TargetSummary(itrial).gaitResp_rawRT = [gaitD(:).response_rawRT_fromsmry];
        catch
        end
        
        
        % save the detailed gait info per trial in structure as well.
        HeadPos(itrial).gaitData = gaitD;
        trial_TargetSummary(itrial).gaitData = gaitD;
        
      
        
    end %trial
    
    %% as an alternative to the below. store all the rel info (per gait) in a big table.
    % that way we can also perform regressions etc., with greater ease
    % (using col headers).
   % here we specify the column names, and datatypes.
   
   
   % this can be performed the same way, for both single and dual gait
   % data, so packaged in a function
    cfg.subjID = subjID; % used in rejTrials_questv1
    cfg.nGait =1;
    cfg.HeadPos= HeadPos; % these 3 used for debugging plots:
    cfg.TargState= TargState;
    cfg.clickState= clickState;
   Ppant_gaitData = createGaitTableData(trial_TargetSummary, cfg);
   
   
    %% sanity check plot:
    
    tmp = Ppant_gaitData.clickPcnt; % compare to the rpcnts collected in plotdebuggait above.    
    rpcnts = tmp(~isnan(tmp));
    tmpT = Ppant_gaitData.targPosPcnt;
    %%
    clf; subplot(211);histogram(tmpT,100)
    subplot(212); histogram(allRpcnt,100); % comparison to check table is correct.
    hold on; histogram(rpcnts,100);
       %% for all trials, compute the head pos per time point,
    expindx= [HeadPos(:).isPrac];
    nprac= length(find(expindx>0));
    
    ntrials = size(HeadPos,2) - nprac;
    [PFX_headY]= deal(zeros(ntrials,100));
    
    %% also store all un resampled gait data, into a large vector.
    PFX_headYraw = zeros(1,length(trialTime)); % will fill
%     PFX_headYnorm = zeros(1,length(trialTime)); % will fill
    PFX_headYresamp = zeros(1,100); % will fill
   gcounter=1;
    for itrial= 1:size(trial_TargetSummary,2)
        if HeadPos(itrial).isPrac ||  HeadPos(itrial).isStationary
            continue
        end
        
         %% subj specific trial rejection
        skip=0;
        rejTrials_questv1; %toggles 'skip' based on bad trial ID               
        if skip==1
            continue
        end
        
         
         nGaits = length(trial_TargetSummary(itrial).gaitData);
        allgaits = 1:nGaits;
       % omit first and last gaitcycles from each trial?
       usegaits = allgaits(3:end-2);
        
        %data of interest is the resampled gait (1:100) with a position of
        %the targ (now classified as correct or no).
        TrialY= trial_TargetSummary(itrial).gaitHeadY(usegaits,:);       
        PFX_headY(itrial,:)= mean(TrialY,1);
        
        %slow!
        for ig = usegaits
            storemeraw = trial_TargetSummary(itrial).gaitData(ig).Head_Yraw;
              storemeresamp = trial_TargetSummary(itrial).gaitData(ig).Head_Y_resampled;
            PFX_headYraw(gcounter,1:length(storemeraw)) = storemeraw;  
            PFX_headYresamp(gcounter,1:length(storemeresamp)) = storemeresamp;  
            
       gcounter=gcounter+1;
        end
        
        
        
    end % all trials
    %%
%     %remove zeros from raw data for plotting
%     PFX_headYraw(PFX_headYraw==0)=nan;% sanity check:
%     clf;
%     usexvec=trialTime(1:size(PFX_headYraw,2));
%     subplot(121);
%     plot(usexvec, PFX_headYraw');
%     ylabel('Head height (raw) [m]')
%     xlabel('Time [s]')
%      set(gca, 'fontsize', 15); shg
%     subplot(122);    
%     plot(1:100, PFX_headYresamp'); ylim([0 1])
%     ylabel('Head height (resampled) [a.u]')
%     xlabel('% gait completion')
%     set(gcf, 'color', 'w'); set(gca, 'fontsize', 15); shg
    %%
%         disp([' All targs recorded for participant' num2str(ippant) '=' num2str(allts)]);
    save(savename, 'HeadPos', 'PFX_headY', 'trial_TargetSummary',...
        'Ppant_gaitData','-append');
end % subject

